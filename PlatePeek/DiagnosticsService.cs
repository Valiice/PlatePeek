using System.Text.Json;
using PlatePeek.Core;

namespace PlatePeek;

public sealed class DiagnosticsService
{
    private const int MaxRetainedDumps = 20;

    private readonly Configuration config;
    private readonly CaptureService capture;
    private readonly SheetResolver resolver;

    public DiagnosticsService(Configuration config, CaptureService capture, SheetResolver resolver)
    {
        this.config = config;
        this.capture = capture;
        this.resolver = resolver;
        capture.SnapshotCaptured += OnSnapshot;
    }

    private void OnSnapshot(PlateSnapshot snapshot)
    {
        if (!config.DebugDumps)
            return;

        try
        {
            var view = resolver.Resolve(snapshot);
            var dump = new
            {
                view.Snapshot,
                view.JobName,
                view.TitleName,
                view.PreferredJobName,
                view.GrandCompanyName,
                view.PoseName,
                view.ExpressionName,
                view.DesignItems,
                view.PortraitItems,
                view.GearItems,
                view.PlayStyleNames,
                PresetString = PortraitPresetCodec.Encode(snapshot.Portrait),
                TextSummary = PlateTextSummary.Format(view),
            };

            var dir = Path.Combine(Services.PluginInterface.GetPluginConfigDirectory(), "dumps");
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(dump, PlateSnapshotJson.Options);
            File.WriteAllText(Path.Combine(dir, "latest.json"), json);

            var safeName = Sanitize(snapshot.Name.Replace(' ', '_'));
            File.WriteAllText(Path.Combine(dir, $"{snapshot.CapturedAt:yyyyMMdd-HHmmss}-{safeName}.json"), json);
            PruneOldDumps(dir);
            Services.Log.Info($"[Dump] wrote plate dump for {snapshot.Name} ({(snapshot.IsSelf ? "self" : "other")})");
        }
        catch (Exception ex)
        {
            Services.Log.Error(ex, "[Dump] failed");
        }
    }

    /// <summary>Keeps only the newest <see cref="MaxRetainedDumps"/> timestamped dump files,
    /// so DebugDumps doesn't grow the dumps directory unbounded over a play
    /// session. latest.json is untouched. Filenames sort correctly by name since the
    /// yyyyMMdd-HHmmss timestamp prefix is fixed-width and zero-padded.</summary>
    private static void PruneOldDumps(string dir)
    {
        var stale = Directory.GetFiles(dir, "*.json")
            .Where(f => !string.Equals(Path.GetFileName(f), "latest.json", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal)
            .Skip(MaxRetainedDumps);

        foreach (var file in stale)
            File.Delete(file);
    }

    /// <summary>Strips characters that are invalid in a filename (beyond the space-to-underscore
    /// swap already applied), since player/FC names can contain arbitrary Unicode punctuation.</summary>
    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return invalid.Length == 0 ? name : string.Concat(name.Where(c => Array.IndexOf(invalid, c) < 0));
    }

    public void RunSelfTest()
    {
        Services.Log.Info("[SELFTEST] starting");
        Check("codec round-trip", () =>
        {
            var sample = new PortraitSnapshot { BannerTimeline = 305, CameraZoom = 100, BannerBg = 1, BannerFrame = 2, BannerDecoration = 3 };
            return PortraitPresetCodec.Decode(PortraitPresetCodec.Encode(sample)) == sample;
        });
        Check("sheets load", () =>
            Services.DataManager.GetExcelSheet<Lumina.Excel.Sheets.CharaCardBase>().Count > 0 &&
            Services.DataManager.GetExcelSheet<Lumina.Excel.Sheets.BannerBg>().Count > 0 &&
            Services.DataManager.GetExcelSheet<Lumina.Excel.Sheets.BannerTimeline>().Count > 0);
        Check("unlock check runs", () =>
        {
            // Condition 0 must always be "unlocked"; a bogus id must not throw.
            using var u = new UnlockService();
            return u.GetUnlockState(0) == true && u.GetUnlockState(0xFFFF) != true;
        });
        Check("plate capture (needs a plate open)", () =>
        {
            var snapshot = capture.CaptureNow();
            if (snapshot == null)
                return false;
            var view = resolver.Resolve(snapshot);
            return view.DesignItems.Count > 0 && view.Snapshot.Name.Length > 0;
        });
        Services.Log.Info("[SELFTEST] done");
        Services.ChatGui.Print("[PlatePeek] Selftest complete - results in /xllog (filter: SELFTEST).");
    }

    private static void Check(string name, Func<bool> check)
    {
        try
        {
            Services.Log.Info(check() ? $"[SELFTEST] PASS {name}" : $"[SELFTEST] FAIL {name}");
        }
        catch (Exception ex)
        {
            Services.Log.Error(ex, $"[SELFTEST] ERROR {name}");
        }
    }
}
