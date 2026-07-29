namespace PlatePeek.Core;

/// <summary>
/// Reads and writes the portrait preset clipboard string format used by
/// HaselTweaks' Portrait Helper (magic "HTPS", version 1), reimplemented
/// here for interoperability. Layout is little-endian, 58 bytes total.
/// </summary>
public static class PortraitPresetCodec
{
    private const int Magic = 0x53505448; // "HTPS"
    private const ushort Version = 1;

    public static string Encode(PortraitSnapshot p)
    {
        using var stream = new MemoryStream(64);
        using var writer = new BinaryWriter(stream);

        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(p.CameraPositionX);
        writer.Write(p.CameraPositionY);
        writer.Write(p.CameraPositionZ);
        writer.Write(p.CameraPositionW);
        writer.Write(p.CameraTargetX);
        writer.Write(p.CameraTargetY);
        writer.Write(p.CameraTargetZ);
        writer.Write(p.CameraTargetW);
        writer.Write(p.ImageRotation);
        writer.Write(p.CameraZoom);
        writer.Write(p.BannerTimeline);
        writer.Write(p.AnimationProgress);
        writer.Write(p.Expression);
        writer.Write(p.HeadDirectionX);
        writer.Write(p.HeadDirectionY);
        writer.Write(p.EyeDirectionX);
        writer.Write(p.EyeDirectionY);
        writer.Write(p.DirectionalLightingColorRed);
        writer.Write(p.DirectionalLightingColorGreen);
        writer.Write(p.DirectionalLightingColorBlue);
        writer.Write(p.DirectionalLightingBrightness);
        writer.Write(p.DirectionalLightingVerticalAngle);
        writer.Write(p.DirectionalLightingHorizontalAngle);
        writer.Write(p.AmbientLightingColorRed);
        writer.Write(p.AmbientLightingColorGreen);
        writer.Write(p.AmbientLightingColorBlue);
        writer.Write(p.AmbientLightingBrightness);
        writer.Write(p.BannerBg);
        writer.Write(p.BannerFrame);
        writer.Write(p.BannerDecoration);
        writer.Flush();

        return Convert.ToBase64String(stream.ToArray());
    }

    public static PortraitSnapshot? Decode(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        byte[] raw;
        try
        {
            raw = Convert.FromBase64String(input);
        }
        catch (FormatException)
        {
            return null;
        }

        if (raw.Length < 58)
            return null;

        using var stream = new MemoryStream(raw);
        using var reader = new BinaryReader(stream);

        if (reader.ReadInt32() != Magic)
            return null;
        if (reader.ReadUInt16() != Version)
            return null;

        return new PortraitSnapshot
        {
            CameraPositionX = reader.ReadHalf(),
            CameraPositionY = reader.ReadHalf(),
            CameraPositionZ = reader.ReadHalf(),
            CameraPositionW = reader.ReadHalf(),
            CameraTargetX = reader.ReadHalf(),
            CameraTargetY = reader.ReadHalf(),
            CameraTargetZ = reader.ReadHalf(),
            CameraTargetW = reader.ReadHalf(),
            ImageRotation = reader.ReadInt16(),
            CameraZoom = reader.ReadByte(),
            BannerTimeline = reader.ReadUInt16(),
            AnimationProgress = reader.ReadSingle(),
            Expression = reader.ReadByte(),
            HeadDirectionX = reader.ReadHalf(),
            HeadDirectionY = reader.ReadHalf(),
            EyeDirectionX = reader.ReadHalf(),
            EyeDirectionY = reader.ReadHalf(),
            DirectionalLightingColorRed = reader.ReadByte(),
            DirectionalLightingColorGreen = reader.ReadByte(),
            DirectionalLightingColorBlue = reader.ReadByte(),
            DirectionalLightingBrightness = reader.ReadByte(),
            DirectionalLightingVerticalAngle = reader.ReadInt16(),
            DirectionalLightingHorizontalAngle = reader.ReadInt16(),
            AmbientLightingColorRed = reader.ReadByte(),
            AmbientLightingColorGreen = reader.ReadByte(),
            AmbientLightingColorBlue = reader.ReadByte(),
            AmbientLightingBrightness = reader.ReadByte(),
            BannerBg = reader.ReadUInt16(),
            BannerFrame = reader.ReadUInt16(),
            BannerDecoration = reader.ReadUInt16(),
        };
    }
}
