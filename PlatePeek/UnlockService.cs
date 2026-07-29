using System.Text.Json;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.Exd;
using Lumina.Excel.Sheets;

namespace PlatePeek;

/// <summary>
/// Answers "does this character own X" via BannerCondition unlock states.
/// The native check only works while the game has the BannerCondition sheet
/// resident (its own editors load it), so whenever one of those editors opens
/// we sweep every condition into a per-character cache persisted to disk.
/// </summary>
public sealed unsafe class UnlockService : IDisposable
{
    private static readonly string[] HarvestAddons = ["BannerEditor", "CharaCardEditMenu", "CharaCardDesignSetting"];

    private Dictionary<uint, uint>? conditionRowIndexById;
    private Dictionary<uint, bool> cache = new();
    private ulong cacheContentId;

    /// <summary>Raised after a harvest updates the cache, so open UI can re-resolve.</summary>
    public event System.Action? CacheUpdated;

    public UnlockService()
    {
        foreach (var addon in HarvestAddons)
            Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, addon, OnEditorOpened);
    }

    public bool? GetUnlockState(uint conditionRowId)
    {
        if (conditionRowId == 0)
            return true;

        EnsureCacheLoaded();

        var live = LiveCheck(conditionRowId);
        if (live != null)
        {
            cache[conditionRowId] = live.Value;
            return live;
        }

        return cache.TryGetValue(conditionRowId, out var cached) ? cached : null;
    }

    public bool IsKnownLocked(uint conditionRowId) => GetUnlockState(conditionRowId) == false;

    /// <summary>Best-effort unlock source from BannerCondition's typed criteria,
    /// with a lookup URL (FFXIV Collect / Garland Tools) when one makes sense.
    /// UnlockType values follow EXDSchema: type1 5=Emote 6=Companion 7=Mount;
    /// type2 11=Achievement, 5/6/8/12=Item, 2=Quest.</summary>
    public (string? Hint, string? Url) GetUnlockInfo(uint conditionRowId)
    {
        if (conditionRowId == 0)
            return (null, null);

        if (!Services.DataManager.GetExcelSheet<BannerCondition>().TryGetRow(conditionRowId, out var cond))
            return (null, null);

        switch (cond.UnlockType1)
        {
            case 5 when cond.UnlockCriteria1[0].TryGetValue<Emote>(out var emote) && emote.RowId != 0:
                return ($"Emote: {emote.Name.ExtractText()}", $"https://ffxivcollect.com/emotes/{emote.RowId}");
            case 6 when cond.UnlockCriteria1[0].TryGetValue<Companion>(out var minion) && minion.RowId != 0:
                return ($"Minion: {minion.Singular.ExtractText()}", $"https://ffxivcollect.com/minions/{minion.RowId}");
            case 7 when cond.UnlockCriteria1[0].TryGetValue<Mount>(out var mount) && mount.RowId != 0:
                return ($"Mount: {mount.Singular.ExtractText()}", $"https://ffxivcollect.com/mounts/{mount.RowId}");
        }

        switch (cond.UnlockType2)
        {
            case 11 when cond.UnlockCriteria2.TryGetValue<Achievement>(out var achievement) && achievement.RowId != 0:
                return ($"Achievement: {achievement.Name.ExtractText()}", $"https://ffxivcollect.com/achievements/{achievement.RowId}");
            case 5 or 6 or 8 or 12 when cond.UnlockCriteria2.TryGetValue<Item>(out var item) && item.RowId != 0:
                return ($"Item: {item.Name.ExtractText()}", $"https://www.garlandtools.org/db/#item/{item.RowId}");
            case 2 when cond.UnlockCriteria2.TryGetValue<Quest>(out var quest) && quest.RowId != 0:
                return ($"Quest: {quest.Name.ExtractText()}", null);
        }

        return (null, null);
    }

    private void OnEditorOpened(AddonEvent type, AddonArgs args)
    {
        try
        {
            HarvestAll();
        }
        catch (Exception ex)
        {
            Services.Log.Error(ex, "[Unlock] harvest failed");
        }
    }

    private void HarvestAll()
    {
        EnsureCacheLoaded();
        EnsureIndexMap();

        var updated = 0;
        var checkedCount = 0;
        foreach (var (rowId, index) in conditionRowIndexById!)
        {
            if (rowId == 0)
                continue;

            var row = ExdModule.GetBannerConditionByIndex(index);
            if (row == null)
                continue; // sheet (or this page) not resident; keep whatever we knew

            checkedCount++;
            var unlocked = ExdModule.GetBannerConditionUnlockState(row) == 0;
            if (!cache.TryGetValue(rowId, out var old) || old != unlocked)
            {
                cache[rowId] = unlocked;
                updated++;
            }
        }

        if (checkedCount == 0)
        {
            Services.Log.Debug("[Unlock] harvest skipped - sheet not resident");
            return;
        }

        SaveCache();
        Services.Log.Debug($"[Unlock] harvested {checkedCount} conditions ({updated} changed) for character {cacheContentId:X}");

        if (updated > 0)
            CacheUpdated?.Invoke();
    }

    private bool? LiveCheck(uint conditionRowId)
    {
        EnsureIndexMap();
        if (!conditionRowIndexById!.TryGetValue(conditionRowId, out var rowIndex))
            return null;

        var row = ExdModule.GetBannerConditionByIndex(rowIndex);
        if (row == null)
            return null;

        return ExdModule.GetBannerConditionUnlockState(row) == 0;
    }

    private void EnsureIndexMap()
    {
        if (conditionRowIndexById != null)
            return;

        // GetBannerConditionByIndex takes the row's ordinal position, not its id.
        // Lumina enumerates rows in EXD file order, which matches the native
        // ordinal for this sheet (verified in-game against the harvest).
        conditionRowIndexById = new Dictionary<uint, uint>();
        uint i = 0;
        foreach (var row in Services.DataManager.GetExcelSheet<BannerCondition>())
            conditionRowIndexById[row.RowId] = i++;
    }

    private void EnsureCacheLoaded()
    {
        var control = Control.Instance();
        var localPlayer = control != null ? control->LocalPlayer : null;
        if (localPlayer == null)
            return;

        var contentId = localPlayer->ContentId;
        if (contentId == 0 || contentId == cacheContentId)
            return;

        cacheContentId = contentId;
        cache = new Dictionary<uint, bool>();
        try
        {
            var path = CachePath(contentId);
            if (File.Exists(path))
            {
                cache = JsonSerializer.Deserialize<Dictionary<uint, bool>>(File.ReadAllText(path)) ?? new Dictionary<uint, bool>();
                Services.Log.Debug($"[Unlock] loaded {cache.Count} cached conditions for character {contentId:X}");
            }
        }
        catch (Exception ex)
        {
            Services.Log.Error(ex, "[Unlock] cache load failed - discarding the file");
            try
            {
                File.Delete(CachePath(contentId));
            }
            catch
            {
                // Nothing sensible left to do; the next harvest rewrites it anyway.
            }
        }
    }

    private void SaveCache()
    {
        if (cacheContentId == 0)
            return;

        try
        {
            File.WriteAllText(CachePath(cacheContentId), JsonSerializer.Serialize(cache));
        }
        catch (Exception ex)
        {
            Services.Log.Error(ex, "[Unlock] cache save failed");
        }
    }

    private static string CachePath(ulong contentId)
    {
        var dir = Services.PluginInterface.GetPluginConfigDirectory();
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"unlocks-{contentId:X}.json");
    }

    public void Dispose()
    {
        Services.AddonLifecycle.UnregisterListener(OnEditorOpened);
    }
}
