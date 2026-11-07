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
        sb.AppendLine($"Pose: {view.PoseName} (frame {s.Portrait.AnimationProgress.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)})");
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

    /// <summary>24-bit hour mask (bit 0 = 00:00) as ranges: "00:00-06:00, 18:00-24:00".</summary>
    public static string FormatActiveHours(uint bitmask)
    {
        bitmask &= 0xFFFFFF;
        if (bitmask == 0)
            return "none set";
        if (bitmask == 0xFFFFFF)
            return "all day";

        var parts = new List<string>();
        for (var hour = 0; hour < 24; hour++)
        {
            if ((bitmask & (1u << hour)) == 0)
                continue;
            var start = hour;
            while (hour + 1 < 24 && (bitmask & (1u << (hour + 1))) != 0)
                hour++;
            parts.Add($"{start:00}:00-{hour + 1:00}:00");
        }
        return string.Join(", ", parts);
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
