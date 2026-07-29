using Lumina.Excel.Sheets;
using PlatePeek.Core;

namespace PlatePeek;

public sealed class SheetResolver
{
    private readonly UnlockService unlocks;

    public SheetResolver(UnlockService unlocks)
    {
        this.unlocks = unlocks;
    }

    public ResolvedPlateView Resolve(PlateSnapshot s)
    {
        var designItems = new List<ResolvedItem>();

        // Base plate
        if (Services.DataManager.GetExcelSheet<CharaCardBase>().TryGetRow(s.BasePlate, out var basePlate))
            designItems.Add(MakeItem("Base Plate", s.BasePlate, basePlate.Name.ExtractText(),
                basePlate.UnlockCondition.RowId, (uint)basePlate.Image));

        // Top/bottom borders
        if (s.TopBorder != 0 && Services.DataManager.GetExcelSheet<CharaCardHeader>().TryGetRow(s.TopBorder, out var top))
            designItems.Add(MakeItem("Top Border", s.TopBorder, top.Name.ExtractText(),
                top.UnlockCondition.RowId, (uint)top.TopImage));
        if (s.BottomBorder != 0 && Services.DataManager.GetExcelSheet<CharaCardHeader>().TryGetRow(s.BottomBorder, out var bottom))
            designItems.Add(MakeItem("Bottom Border", s.BottomBorder, bottom.Name.ExtractText(),
                bottom.UnlockCondition.RowId, (uint)bottom.BottomImage));

        // Typed decorations (backing, pattern overlay, portrait frame, plate frame, accent)
        var decoSheet = Services.DataManager.GetExcelSheet<CharaCardDecoration>();
        foreach (var decoId in s.Decorations)
        {
            if (!decoSheet.TryGetRow(decoId, out var deco))
                continue;
            var category = (PlateDecorationType)deco.Component switch
            {
                PlateDecorationType.Backing => "Backing",
                PlateDecorationType.PatternOverlay => "Pattern Overlay",
                PlateDecorationType.PortraitFrame => "Portrait Frame",
                PlateDecorationType.PlateFrame => "Plate Frame",
                PlateDecorationType.Accent => "Accent",
                _ => "Decoration",
            };
            designItems.Add(MakeItem(category, decoId, deco.Name.ExtractText(),
                deco.UnlockCondition.RowId, (uint)deco.Image));
        }

        // Portrait bg / frame / accent
        var portraitItems = new List<ResolvedItem>();
        if (Services.DataManager.GetExcelSheet<BannerBg>().TryGetRow(s.Portrait.BannerBg, out var bg))
            portraitItems.Add(MakeItem("Background", s.Portrait.BannerBg, bg.Name.ExtractText(),
                bg.UnlockCondition.RowId, (uint)bg.Icon));
        if (s.Portrait.BannerFrame != 0 && Services.DataManager.GetExcelSheet<BannerFrame>().TryGetRow(s.Portrait.BannerFrame, out var frame))
            portraitItems.Add(MakeItem("Frame", s.Portrait.BannerFrame, frame.Name.ExtractText(),
                frame.UnlockCondition.RowId, (uint)frame.Icon));
        if (s.Portrait.BannerDecoration != 0 && Services.DataManager.GetExcelSheet<BannerDecoration>().TryGetRow(s.Portrait.BannerDecoration, out var accent))
            portraitItems.Add(MakeItem("Accent", s.Portrait.BannerDecoration, accent.Name.ExtractText(),
                accent.UnlockCondition.RowId, (uint)accent.Icon));

        // Pose (BannerTimeline): include unlock state; name may live on Action/Emote
        var poseName = "Unknown";
        var poseUnlocked = (bool?)null;
        if (Services.DataManager.GetExcelSheet<BannerTimeline>().TryGetRow(s.Portrait.BannerTimeline, out var timeline))
        {
            poseName = timeline.Name.ExtractText();
            if (string.IsNullOrEmpty(poseName) && timeline.Type != 0)
            {
                if (timeline.AdditionalData.TryGetValue<Lumina.Excel.Sheets.Action>(out var action))
                    poseName = action.Name.ExtractText();
                else if (timeline.AdditionalData.TryGetValue<Emote>(out var emote))
                    poseName = emote.Name.ExtractText();
            }
            poseUnlocked = unlocks.GetUnlockState(timeline.UnlockCondition.RowId);
            var (poseHint, poseUrl) = poseUnlocked == true ? (null, null) : unlocks.GetUnlockInfo(timeline.UnlockCondition.RowId);
            if (poseUnlocked != true && poseUrl == null && poseName.Length > 0)
                poseUrl = SearchUrl(poseName, "portrait pose");
            portraitItems.Add(new ResolvedItem("Pose", s.Portrait.BannerTimeline, poseName, poseUnlocked,
                poseHint, (uint)timeline.Icon, poseUrl));
        }

        // Gear
        var gearItems = new List<ResolvedItem>();
        var itemSheet = Services.DataManager.GetExcelSheet<Item>();
        var stainSheet = Services.DataManager.GetExcelSheet<Stain>();
        foreach (var piece in s.Gear)
        {
            if (!itemSheet.TryGetRow(piece.ItemId, out var item))
                continue;
            var name = item.Name.ExtractText();
            var dyes = new List<string>();
            if (piece.Stain0 != 0 && stainSheet.TryGetRow(piece.Stain0, out var stain0))
                dyes.Add(stain0.Name.ExtractText());
            if (piece.Stain1 != 0 && stainSheet.TryGetRow(piece.Stain1, out var stain1))
                dyes.Add(stain1.Name.ExtractText());
            if (dyes.Count > 0)
                name += $" ({string.Join(", ", dyes)})";
            gearItems.Add(new ResolvedItem("Item", piece.ItemId, name, null, null, item.Icon));
        }
        foreach (var glassesId in new[] { s.Glasses0, s.Glasses1 })
        {
            if (glassesId != 0 && Services.DataManager.GetExcelSheet<Glasses>().TryGetRow(glassesId, out var glasses))
                gearItems.Add(new ResolvedItem("Glasses", glassesId, glasses.Name.ExtractText(), null, null, (uint)glasses.Icon));
        }

        // Profile names
        var playStyleNames = new List<string>();
        var playStyleSheet = Services.DataManager.GetExcelSheet<CharaCardPlayStyle>();
        foreach (var ps in s.PlayStyles)
        {
            if (playStyleSheet.TryGetRow(ps, out var row))
                playStyleNames.Add(row.Name.ExtractText());
        }

        var worldName = Services.DataManager.GetExcelSheet<World>().TryGetRow(s.WorldId, out var world)
            ? world.Name.ExtractText() : "";
        var jobName = Services.DataManager.GetExcelSheet<ClassJob>().TryGetRow(s.ClassJobId, out var job)
            ? job.Name.ExtractText() : "";
        var preferredJobName = Services.DataManager.GetExcelSheet<ClassJob>().TryGetRow(s.PreferredClassJobId, out var prefJob)
            ? prefJob.Name.ExtractText() : "";
        var titleName = s.TitleId != 0 && Services.DataManager.GetExcelSheet<Title>().TryGetRow(s.TitleId, out var title)
            ? title.Masculine.ExtractText() : "";
        var gcName = s.GrandCompany != 0 && Services.DataManager.GetExcelSheet<GrandCompany>().TryGetRow(s.GrandCompany, out var gc)
            ? gc.Name.ExtractText() : "";

        var expressionName = s.Portrait.Expression == 0 ? "None" : $"#{s.Portrait.Expression}";

        return new ResolvedPlateView
        {
            Snapshot = s with { WorldName = worldName },
            JobName = jobName,
            TitleName = titleName,
            PreferredJobName = preferredJobName,
            GrandCompanyName = gcName,
            PoseName = poseName,
            ExpressionName = expressionName,
            DesignItems = designItems,
            PortraitItems = portraitItems,
            GearItems = gearItems,
            PlayStyleNames = playStyleNames,
        };
    }

    private static string SearchUrl(string name, string category)
        => $"https://www.google.com/search?q={Uri.EscapeDataString($"FFXIV {name} {category} unlock")}";

    private ResolvedItem MakeItem(string category, uint rowId, string name, uint unlockConditionRowId, uint iconId)
    {
        var unlocked = unlocks.GetUnlockState(unlockConditionRowId);
        var (hint, url) = unlocked == true ? (null, null) : unlocks.GetUnlockInfo(unlockConditionRowId);
        if (unlocked != true && url == null && name.Length > 0)
            url = SearchUrl(name, category);
        return new ResolvedItem(category, rowId, name.Length > 0 ? name : $"#{rowId}", unlocked, hint, iconId, url);
    }
}
