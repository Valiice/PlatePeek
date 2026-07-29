using PlatePeek.Core;
using Xunit;

namespace PlatePeek.Tests;

public class PlateTextSummaryTests
{
    private static ResolvedPlateView SampleView()
    {
        var snapshot = new PlateSnapshot
        {
            Name = "Va Li",
            WorldName = "Alpha",
            Level = 100,
            InvertPortraitPlacement = false,
            Portrait = new PortraitSnapshot { BannerTimeline = 305, AnimationProgress = 30.9f, CameraZoom = 100 },
        };
        return new ResolvedPlateView
        {
            Snapshot = snapshot,
            JobName = "Warrior",
            TitleName = "Shadowbringer",
            PoseName = "Fighter's Stance",
            ExpressionName = "Neutral",
            DesignItems =
            [
                new ResolvedItem("Base Plate", 12, "Howling Blade", true, null, 190012),
                new ResolvedItem("Backing", 7, "Howling Blade", false, "Achievement: Wolf at the Door", 190107),
            ],
            PortraitItems =
            [
                new ResolvedItem("Background", 71, "Howling Blade", true, null, 190271),
            ],
            GearItems =
            [
                new ResolvedItem("Item", 40001, "Voidvessel Warfists (Metallic Silver)", null, null, 13000),
            ],
            PlayStyleNames = ["Raids", "Trials"],
        };
    }

    [Fact]
    public void Format_ContainsHeaderDesignAndPortraitLines()
    {
        var text = PlateTextSummary.Format(SampleView());

        Assert.Contains("Va Li @ Alpha", text);
        Assert.Contains("Warrior", text);
        Assert.Contains("Base Plate: Howling Blade", text);
        Assert.Contains("Backing: Howling Blade", text);
        Assert.Contains("(locked - Achievement: Wolf at the Door)", text);
        Assert.Contains("Background: Howling Blade", text);
        Assert.Contains("Pose: Fighter's Stance", text);
        Assert.Contains("Voidvessel Warfists", text);
        Assert.Contains("Playstyles: Raids, Trials", text);
    }

    [Fact]
    public void Format_MarksUnlockedItemsWithoutLockNote()
    {
        var text = PlateTextSummary.Format(SampleView());
        var basePlateLine = text.Split('\n').First(l => l.Contains("Base Plate:"));
        Assert.DoesNotContain("locked", basePlateLine);
    }
}
