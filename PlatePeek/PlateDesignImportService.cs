using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using PlatePeek.Core;

namespace PlatePeek;

/// <summary>
/// Writes a captured plate design into the player's own open Edit Plate Design
/// window. The editor's live state is AgentCharaCard.Storage.PlateDesign (the
/// game snapshots it into BackupPlateDesign and restores on cancel), so values
/// written here go through the game's normal save path when the player hits
/// Save. Known-locked layers are skipped; the server validates everything on
/// save regardless.
/// </summary>
public sealed unsafe class PlateDesignImportService
{
    private const string DesignAddonName = "CharaCardDesignSetting";

    private readonly UnlockService unlocks;

    public PlateDesignImportService(UnlockService unlocks)
    {
        this.unlocks = unlocks;
    }

    public bool IsDesignEditorOpen()
    {
        if (Services.GameGui.GetAddonByName(DesignAddonName) == nint.Zero)
            return false;

        var agent = AgentCharaCard.Instance();
        return agent != null && agent->Data != null && agent->Data->CanEdit;
    }

    public bool TryImport(DesignSnapshot design, out string message)
    {
        if (!IsDesignEditorOpen())
        {
            message = "Plate design editor is not open.";
            return false;
        }

        var data = AgentCharaCard.Instance()->Data;
        var skipped = new List<string>();

        if (!IsLocked<CharaCardBase>(design.BasePlate))
            data->PlateDesign.BasePlate = design.BasePlate;
        else
            skipped.Add("base plate");

        if (design.TopBorder == 0 || !IsLocked<CharaCardHeader>(design.TopBorder))
            data->PlateDesign.TopBorder = design.TopBorder;
        else
            skipped.Add("top border");

        if (design.BottomBorder == 0 || !IsLocked<CharaCardHeader>(design.BottomBorder))
            data->PlateDesign.BottomBorder = design.BottomBorder;
        else
            skipped.Add("bottom border");

        var decoSheet = Services.DataManager.GetExcelSheet<CharaCardDecoration>();
        var applied = new List<ushort>();
        foreach (var decoId in design.Decorations.Take(5))
        {
            if (decoId == 0)
                continue;
            if (IsLocked<CharaCardDecoration>(decoId))
            {
                var name = decoSheet.TryGetRow(decoId, out var row) ? row.Name.ExtractText() : $"decoration #{decoId}";
                skipped.Add(name.Length > 0 ? name : $"decoration #{decoId}");
                continue;
            }
            applied.Add(decoId);
        }

        for (var i = 0; i < 5; i++)
            data->PlateDesign.Decorations[i] = i < applied.Count ? applied[i] : (ushort)0;
        data->PlateDesign.NumDecorations = (byte)applied.Count;

        data->InvertPortraitPlacement = design.InvertPortraitPlacement;

        Services.Log.Debug(
            $"[DesignImport] base={data->PlateDesign.BasePlate} top={data->PlateDesign.TopBorder} " +
            $"bottom={data->PlateDesign.BottomBorder} decos={applied.Count} invert={data->InvertPortraitPlacement}");

        message = skipped.Count == 0
            ? "Design imported. Check the preview and hit Save."
            : $"Design imported. Skipped locked: {string.Join(", ", skipped)}. Hit Save to keep the rest.";
        return true;
    }

    private bool IsLocked<TSheet>(uint rowId)
        where TSheet : struct, Lumina.Excel.IExcelRow<TSheet>
    {
        if (!Services.DataManager.GetExcelSheet<TSheet>().TryGetRow(rowId, out var row))
            return false;

        var conditionRowId = (object)row switch
        {
            CharaCardBase basePlate => basePlate.UnlockCondition.RowId,
            CharaCardHeader header => header.UnlockCondition.RowId,
            CharaCardDecoration deco => deco.UnlockCondition.RowId,
            _ => 0u,
        };
        return unlocks.IsKnownLocked(conditionRowId);
    }
}
