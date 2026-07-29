namespace PlatePeek.Core;

/// <summary>
/// A managed copy of the game's ExportedPortraitData, plus BannerFrame and
/// BannerDecoration which the game stores next to it rather than inside it.
/// Field types mirror the binary preset format exactly.
/// </summary>
public sealed record PortraitSnapshot
{
    public Half CameraPositionX { get; init; }
    public Half CameraPositionY { get; init; }
    public Half CameraPositionZ { get; init; }
    public Half CameraPositionW { get; init; }
    public Half CameraTargetX { get; init; }
    public Half CameraTargetY { get; init; }
    public Half CameraTargetZ { get; init; }
    public Half CameraTargetW { get; init; }
    public short ImageRotation { get; init; }
    public byte CameraZoom { get; init; }
    public ushort BannerTimeline { get; init; }
    public float AnimationProgress { get; init; }
    public byte Expression { get; init; }
    public Half HeadDirectionX { get; init; }
    public Half HeadDirectionY { get; init; }
    public Half EyeDirectionX { get; init; }
    public Half EyeDirectionY { get; init; }
    public byte DirectionalLightingColorRed { get; init; }
    public byte DirectionalLightingColorGreen { get; init; }
    public byte DirectionalLightingColorBlue { get; init; }
    public byte DirectionalLightingBrightness { get; init; }
    public short DirectionalLightingVerticalAngle { get; init; }
    public short DirectionalLightingHorizontalAngle { get; init; }
    public byte AmbientLightingColorRed { get; init; }
    public byte AmbientLightingColorGreen { get; init; }
    public byte AmbientLightingColorBlue { get; init; }
    public byte AmbientLightingBrightness { get; init; }
    public ushort BannerBg { get; init; }
    public ushort BannerFrame { get; init; }
    public ushort BannerDecoration { get; init; }
}
