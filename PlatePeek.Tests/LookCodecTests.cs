using PlatePeek.Core;
using Xunit;

namespace PlatePeek.Tests;

public class LookCodecTests
{
    private static PlateSnapshot Sample() => new()
    {
        BasePlate = 160,
        TopBorder = 95,
        InvertPortraitPlacement = true,
        Decorations = [689, 682],
        Portrait = new PortraitSnapshot { BannerTimeline = 305, CameraZoom = 100, BannerBg = 71, CameraPositionX = (Half)1.5f },
    };

    [Fact]
    public void RoundTrip_CarriesDesignAndPortrait()
    {
        var decoded = LookCodec.Decode(LookCodec.Encode(Sample()));
        Assert.NotNull(decoded);
        Assert.Equal(160, decoded.Value.Design!.BasePlate);
        Assert.Equal([689, 682], decoded.Value.Design.Decorations);
        Assert.Equal(Sample().Portrait, decoded.Value.Portrait);
    }

    [Fact]
    public void Decode_AcceptsBareDesignCode_AndBarePortraitPreset()
    {
        var design = LookCodec.Decode(PlateDesignCodec.Encode(DesignSnapshot.FromPlate(Sample())));
        Assert.NotNull(design?.Design);
        Assert.Null(design?.Portrait);

        var portrait = LookCodec.Decode(" " + PortraitPresetCodec.Encode(Sample().Portrait) + "\n");
        Assert.Null(portrait?.Design);
        Assert.Equal(Sample().Portrait, portrait?.Portrait);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage.more garbage")]
    public void Decode_ReturnsNullOnJunk(string? input) => Assert.Null(LookCodec.Decode(input));

    [Fact]
    public void ToSnapshot_FillsDesignAndPortrait()
    {
        var decoded = LookCodec.Decode(LookCodec.Encode(Sample()))!.Value;
        var s = LookCodec.ToSnapshot(decoded.Design, decoded.Portrait);
        Assert.Equal(160, s.BasePlate);
        Assert.Equal(305, s.Portrait.BannerTimeline);
        Assert.Equal("Pasted look", s.Name);
    }

    [Fact]
    public void SnapshotJson_RoundTripsIncludingHalfFields()
    {
        var original = Sample() with { Name = "Va Li", Gear = [new GearPiece { ItemId = 40001, Stain0 = 3 }], Sex = 1 };
        var copy = PlateSnapshotJson.Deserialize(PlateSnapshotJson.Serialize(original));
        Assert.NotNull(copy);
        Assert.Equal("Va Li", copy.Name);
        Assert.Equal(1, copy.Sex);
        Assert.Equal(original.Portrait, copy.Portrait);
        Assert.Equal(40001u, copy.Gear[0].ItemId);
        Assert.Equal(3, copy.Gear[0].Stain0);
        Assert.Equal([689, 682], copy.Decorations);
    }
}
