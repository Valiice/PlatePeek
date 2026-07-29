using System.Text;

namespace PlatePeek.Core;

public static class PlateTextSummary
{
    public static string Format(ResolvedPlateView view)
    {
        var s = view.Snapshot;
        var sb = new StringBuilder();

        sb.AppendLine($"Adventurer Plate: {s.Name} @ {s.WorldName}");
        sb.AppendLine($"Level {s.Level} {view.JobName}" + (view.TitleName.Length > 0 ? $" <{view.TitleName}>" : ""));
        sb.AppendLine();

        sb.AppendLine("[Design]");
        foreach (var item in view.DesignItems)
            sb.AppendLine(FormatItem(item));
        sb.AppendLine($"Invert Portrait Placement: {(s.InvertPortraitPlacement ? "Yes" : "No")}");
        sb.AppendLine();

        sb.AppendLine("[Portrait]");
        foreach (var item in view.PortraitItems)
            sb.AppendLine(FormatItem(item));
        sb.AppendLine($"Pose: {view.PoseName} (frame {s.Portrait.AnimationProgress:0.###})");
        sb.AppendLine($"Expression: {view.ExpressionName}");
        sb.AppendLine($"Camera Zoom: {s.Portrait.CameraZoom}  Rotation: {s.Portrait.ImageRotation}");
        sb.AppendLine();

        if (view.GearItems.Count > 0)
        {
            sb.AppendLine("[Gear]");
            foreach (var item in view.GearItems)
                sb.AppendLine($"- {item.Name}");
            sb.AppendLine();
        }

        if (view.PlayStyleNames.Count > 0)
            sb.AppendLine($"Playstyles: {string.Join(", ", view.PlayStyleNames)}");

        return sb.ToString();
    }

    private static string FormatItem(ResolvedItem item)
    {
        var line = $"{item.Category}: {item.Name}";
        if (item.Unlocked == false)
            line += item.UnlockHint is { Length: > 0 } hint
                ? $" (locked - {hint})"
                : " (locked)";
        return line;
    }
}
