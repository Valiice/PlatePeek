using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using PlatePeek.Core;

namespace PlatePeek;

/// <summary>
/// Writes a captured portrait into the player's own open portrait editor
/// using the game's native import function. Locked backgrounds, frames,
/// accents, and poses are skipped (the server rejects saving them anyway);
/// everything else (camera, lighting, expression, animation frame) applies.
/// </summary>
public sealed unsafe class PortraitImportService
{
    private readonly UnlockService unlocks;

    public PortraitImportService(UnlockService unlocks)
    {
        this.unlocks = unlocks;
    }

    public bool IsEditorOpen()
    {
        var agent = AgentBannerEditor.Instance();
        return agent != null && agent->EditorState != null && agent->EditorState->CharaView != null;
    }

    public bool TryImport(PortraitSnapshot p, out string message)
    {
        if (!IsEditorOpen())
        {
            message = "Portrait editor is not open.";
            return false;
        }

        var state = AgentBannerEditor.Instance()->EditorState;
        var skipped = new List<string>();

        var data = stackalloc ExportedPortraitData[1];
        state->CharaView->ExportPortraitData(data);

        // Ungated values: always apply.
        data->CameraPosition.X = p.CameraPositionX;
        data->CameraPosition.Y = p.CameraPositionY;
        data->CameraPosition.Z = p.CameraPositionZ;
        data->CameraPosition.W = p.CameraPositionW;
        data->CameraTarget.X = p.CameraTargetX;
        data->CameraTarget.Y = p.CameraTargetY;
        data->CameraTarget.Z = p.CameraTargetZ;
        data->CameraTarget.W = p.CameraTargetW;
        data->ImageRotation = p.ImageRotation;
        data->CameraZoom = p.CameraZoom;
        data->AnimationProgress = p.AnimationProgress;
        data->Expression = p.Expression;
        data->HeadDirection.X = p.HeadDirectionX;
        data->HeadDirection.Y = p.HeadDirectionY;
        data->EyeDirection.X = p.EyeDirectionX;
        data->EyeDirection.Y = p.EyeDirectionY;
        data->DirectionalLightingColorRed = p.DirectionalLightingColorRed;
        data->DirectionalLightingColorGreen = p.DirectionalLightingColorGreen;
        data->DirectionalLightingColorBlue = p.DirectionalLightingColorBlue;
        data->DirectionalLightingBrightness = p.DirectionalLightingBrightness;
        data->DirectionalLightingVerticalAngle = p.DirectionalLightingVerticalAngle;
        data->DirectionalLightingHorizontalAngle = p.DirectionalLightingHorizontalAngle;
        data->AmbientLightingColorRed = p.AmbientLightingColorRed;
        data->AmbientLightingColorGreen = p.AmbientLightingColorGreen;
        data->AmbientLightingColorBlue = p.AmbientLightingColorBlue;
        data->AmbientLightingBrightness = p.AmbientLightingBrightness;

        // Unlock-gated values: apply only what the player owns.
        if (TryGetCondition<BannerBg>(p.BannerBg, out var bgCond) && !unlocks.IsKnownLocked(bgCond))
        {
            data->BannerBg = p.BannerBg;
            state->BannerEntry.BannerBg = p.BannerBg;
        }
        else
        {
            skipped.Add("background");
        }

        if (TryGetCondition<BannerTimeline>(p.BannerTimeline, out var poseCond) && !unlocks.IsKnownLocked(poseCond))
        {
            data->BannerTimeline = p.BannerTimeline;
            state->BannerEntry.BannerTimeline = p.BannerTimeline;
        }
        else
        {
            skipped.Add("pose");
        }

        state->BannerEntry.Expression = p.Expression;

        Services.Log.Debug(
            $"[Import] applying: timeline={data->BannerTimeline} progress={data->AnimationProgress:0.##} zoom={data->CameraZoom} " +
            $"bg={data->BannerBg} expr={data->Expression} camX={(float)data->CameraPosition.X:0.##}");
        // Note: no ApplyCameraPositions here. ImportPortraitData applies the camera
        // itself; re-applying afterwards can restore stale positions over the import.
        state->CharaView->ImportPortraitData(data);
        Services.Log.Debug(
            $"[Import] view after import: camPos={state->CharaView->CameraPosition.X:0.##},{state->CharaView->CameraPosition.Y:0.##},{state->CharaView->CameraPosition.Z:0.##} " +
            $"zoom={state->CharaView->CameraZoom} poseJob={state->CharaView->PoseClassJob} bg={state->CharaView->BannerBg}");

        if (TryGetCondition<BannerFrame>(p.BannerFrame, out var frameCond) && !unlocks.IsKnownLocked(frameCond))
            state->SetFrame(p.BannerFrame);
        else
            skipped.Add("frame");

        if (TryGetCondition<BannerDecoration>(p.BannerDecoration, out var accentCond) && !unlocks.IsKnownLocked(accentCond))
            state->SetAccent(p.BannerDecoration);
        else
            skipped.Add("accent");

        state->SetHasChanged(true);

        message = skipped.Count == 0
            ? "Portrait imported. The editor's dropdown labels may lag until you touch them; the render and saved values are correct."
            : $"Portrait imported. Skipped locked: {string.Join(", ", skipped)}.";
        return true;
    }

    private static bool TryGetCondition<TSheet>(uint rowId, out uint conditionRowId)
        where TSheet : struct, Lumina.Excel.IExcelRow<TSheet>
    {
        conditionRowId = 0;
        if (!Services.DataManager.GetExcelSheet<TSheet>().TryGetRow(rowId, out var row))
            return false;

        // All four sheets used here (BannerBg, BannerFrame, BannerDecoration,
        // BannerTimeline) expose UnlockCondition -> BannerCondition.
        conditionRowId = (object)row switch
        {
            BannerBg bg => bg.UnlockCondition.RowId,
            BannerFrame frame => frame.UnlockCondition.RowId,
            BannerDecoration deco => deco.UnlockCondition.RowId,
            BannerTimeline timeline => timeline.UnlockCondition.RowId,
            _ => 0,
        };
        return true;
    }
}
