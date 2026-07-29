using PlatePeek.Core;
using Xunit;

namespace PlatePeek.Tests;

public class PortraitPresetCodecTests
{
    private static PortraitSnapshot Sample() => new()
    {
        CameraPositionX = (Half)1.5f, CameraPositionY = (Half)(-0.25f), CameraPositionZ = (Half)3.0f, CameraPositionW = (Half)1.0f,
        CameraTargetX = (Half)0.5f, CameraTargetY = (Half)1.25f, CameraTargetZ = (Half)(-2.0f), CameraTargetW = (Half)1.0f,
        ImageRotation = -45,
        CameraZoom = 128,
        BannerTimeline = 305,
        AnimationProgress = 30.899f,
        Expression = 3,
        HeadDirectionX = (Half)0.1f, HeadDirectionY = (Half)(-0.2f),
        EyeDirectionX = (Half)0.3f, EyeDirectionY = (Half)0.4f,
        DirectionalLightingColorRed = 255, DirectionalLightingColorGreen = 200, DirectionalLightingColorBlue = 100,
        DirectionalLightingBrightness = 65,
        DirectionalLightingVerticalAngle = 64, DirectionalLightingHorizontalAngle = -30,
        AmbientLightingColorRed = 10, AmbientLightingColorGreen = 20, AmbientLightingColorBlue = 30,
        AmbientLightingBrightness = 80,
        BannerBg = 71,
        BannerFrame = 42,
        BannerDecoration = 17,
    };

    [Fact]
    public void Encode_ProducesBase64Of58Bytes_WithHtpsMagicAndVersion1()
    {
        var bytes = Convert.FromBase64String(PortraitPresetCodec.Encode(Sample()));
        Assert.Equal(58, bytes.Length);
        // "HTPS" magic, little-endian int32 0x53505448
        Assert.Equal((byte)'H', bytes[0]);
        Assert.Equal((byte)'T', bytes[1]);
        Assert.Equal((byte)'P', bytes[2]);
        Assert.Equal((byte)'S', bytes[3]);
        Assert.Equal(1, BitConverter.ToUInt16(bytes, 4));
    }

    [Fact]
    public void Decode_RoundTripsEveryField()
    {
        var original = Sample();
        var decoded = PortraitPresetCodec.Decode(PortraitPresetCodec.Encode(original));
        Assert.NotNull(decoded);
        Assert.Equal(original, decoded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64 !!!")]
    [InlineData("AAAA")] // valid base64, too short
    public void Decode_ReturnsNullOnInvalidInput(string? input)
    {
        Assert.Null(PortraitPresetCodec.Decode(input));
    }

    [Fact]
    public void Decode_ReturnsNullOnWrongMagic()
    {
        var bytes = Convert.FromBase64String(PortraitPresetCodec.Encode(Sample()));
        bytes[0] = (byte)'X';
        Assert.Null(PortraitPresetCodec.Decode(Convert.ToBase64String(bytes)));
    }
}
