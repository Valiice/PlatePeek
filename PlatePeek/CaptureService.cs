using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PlatePeek.Core;

namespace PlatePeek;

public sealed unsafe class CaptureService : IDisposable
{
    private const string AddonName = "CharaCard";

    public PlateSnapshot? Latest { get; private set; }
    public event Action<PlateSnapshot>? SnapshotCaptured;
    public event Action? PlateClosed;

    public CaptureService()
    {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, AddonName, OnAddonEvent);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, AddonName, OnAddonEvent);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, AddonName, OnAddonClose);
    }

    private void OnAddonEvent(AddonEvent type, AddonArgs args)
    {
        try
        {
            var snapshot = CaptureNow();
            if (snapshot == null)
                return;

            Latest = snapshot;
            Services.Log.Debug(
                $"[Capture] {snapshot.Name}@{snapshot.WorldId} plate={snapshot.BasePlate} " +
                $"pose={snapshot.Portrait.BannerTimeline} bg={snapshot.Portrait.BannerBg} self={snapshot.IsSelf}");
            SnapshotCaptured?.Invoke(snapshot);
        }
        catch (Exception ex)
        {
            Services.Log.Error(ex, "[Capture] failed");
        }
    }

    private void OnAddonClose(AddonEvent type, AddonArgs args) => PlateClosed?.Invoke();

    public PlateSnapshot? CaptureNow()
    {
        var agent = AgentCharaCard.Instance();
        if (agent == null || agent->Data == null)
            return null;

        var s = agent->Data;
        if (s->IsNotCreated)
            return null;

        var pd = s->PortraitData;
        var portrait = new PortraitSnapshot
        {
            CameraPositionX = pd.CameraPosition.X,
            CameraPositionY = pd.CameraPosition.Y,
            CameraPositionZ = pd.CameraPosition.Z,
            CameraPositionW = pd.CameraPosition.W,
            CameraTargetX = pd.CameraTarget.X,
            CameraTargetY = pd.CameraTarget.Y,
            CameraTargetZ = pd.CameraTarget.Z,
            CameraTargetW = pd.CameraTarget.W,
            ImageRotation = pd.ImageRotation,
            CameraZoom = pd.CameraZoom,
            BannerTimeline = pd.BannerTimeline,
            AnimationProgress = pd.AnimationProgress,
            Expression = pd.Expression,
            HeadDirectionX = pd.HeadDirection.X,
            HeadDirectionY = pd.HeadDirection.Y,
            EyeDirectionX = pd.EyeDirection.X,
            EyeDirectionY = pd.EyeDirection.Y,
            DirectionalLightingColorRed = pd.DirectionalLightingColorRed,
            DirectionalLightingColorGreen = pd.DirectionalLightingColorGreen,
            DirectionalLightingColorBlue = pd.DirectionalLightingColorBlue,
            DirectionalLightingBrightness = pd.DirectionalLightingBrightness,
            DirectionalLightingVerticalAngle = pd.DirectionalLightingVerticalAngle,
            DirectionalLightingHorizontalAngle = pd.DirectionalLightingHorizontalAngle,
            AmbientLightingColorRed = pd.AmbientLightingColorRed,
            AmbientLightingColorGreen = pd.AmbientLightingColorGreen,
            AmbientLightingColorBlue = pd.AmbientLightingColorBlue,
            AmbientLightingBrightness = pd.AmbientLightingBrightness,
            BannerBg = pd.BannerBg,
            BannerFrame = s->BannerFrame,
            BannerDecoration = s->BannerDecoration,
        };

        var decorations = new List<ushort>();
        foreach (var d in s->PlateDesign.Decorations)
        {
            if (d != 0)
                decorations.Add(d);
        }

        var playStyles = new List<byte>();
        for (var i = 0; i < s->NumPlayStyles && i < 6; i++)
            playStyles.Add(s->PlayStyles[i]);

        var localPlayer = Control.Instance()->LocalPlayer;
        var isSelf = localPlayer != null && localPlayer->ContentId == s->ContentId;

        var charData = s->CharaView.PortraitCharacterData;
        var gear = new List<GearPiece>();
        for (var i = 0; i < 14; i++)
        {
            if (charData.ItemIds[i] == 0)
                continue;
            gear.Add(new GearPiece
            {
                ItemId = charData.ItemIds[i],
                Stain0 = charData.ItemStain0Ids[i],
                Stain1 = charData.ItemStain1Ids[i],
            });
        }

        return new PlateSnapshot
        {
            Name = s->Name.ToString(),
            WorldId = s->WorldId,
            ClassJobId = s->ClassJobId,
            Level = s->Level,
            TitleId = s->TitleId,
            GrandCompany = s->GrandCompany,
            GcRank = s->GcRank,
            FreeCompany = s->FreeCompany.ToString(),
            SearchComment = s->SearchComment.ToString(),
            PreferredClassJobId = s->PreferredClassJobId,
            PlayStyles = playStyles,
            ActiveHoursWeekdays = s->ActiveHoursWeekdays,
            ActiveHoursWeekends = s->ActiveHoursWeekends,
            BasePlate = s->PlateDesign.BasePlate,
            TopBorder = s->PlateDesign.TopBorder,
            BottomBorder = s->PlateDesign.BottomBorder,
            InvertPortraitPlacement = s->InvertPortraitPlacement,
            Decorations = decorations,
            Portrait = portrait,
            Gear = gear,
            Glasses0 = charData.GlassesIds[0],
            Glasses1 = charData.GlassesIds[1],
            GearClassJobId = charData.ClassJobId,
            IsSelf = isSelf,
            WasResetDueToFantasia = s->WasResetDueToFantasia,
            ContentId = s->ContentId,
            CapturedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Dispose()
    {
        Services.AddonLifecycle.UnregisterListener(OnAddonEvent);
        Services.AddonLifecycle.UnregisterListener(OnAddonClose);
    }
}
