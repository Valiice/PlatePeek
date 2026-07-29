using PlatePeek.Core;
using Xunit;

namespace PlatePeek.Tests;

public class PlateDesignCodecTests
{
    private static DesignSnapshot Sample() => new()
    {
        BasePlate = 160,
        TopBorder = 95,
        BottomBorder = 0,
        InvertPortraitPlacement = true,
        Decorations = [689, 682, 706, 717],
    };

    [Fact]
    public void Encode_ProducesBase64WithPpdsMagicAndVersion1()
    {
        var bytes = Convert.FromBase64String(PlateDesignCodec.Encode(Sample()));
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'S', bytes[3]);
        Assert.Equal(1, BitConverter.ToUInt16(bytes, 4));
        Assert.Equal(12 + 4 * 2, bytes.Length);
    }

    [Fact]
    public void Decode_RoundTripsEveryField()
    {
        var original = Sample();
        var decoded = PlateDesignCodec.Decode(PlateDesignCodec.Encode(original));

        Assert.NotNull(decoded);
        Assert.Equal(original.BasePlate, decoded.BasePlate);
        Assert.Equal(original.TopBorder, decoded.TopBorder);
        Assert.Equal(original.BottomBorder, decoded.BottomBorder);
        Assert.Equal(original.InvertPortraitPlacement, decoded.InvertPortraitPlacement);
        Assert.Equal(original.Decorations, decoded.Decorations);
    }

    [Fact]
    public void Decode_RoundTripsEmptyDecorations()
    {
        var decoded = PlateDesignCodec.Decode(PlateDesignCodec.Encode(new DesignSnapshot()));
        Assert.NotNull(decoded);
        Assert.Empty(decoded.Decorations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64 !!!")]
    [InlineData("AAAA")]
    public void Decode_ReturnsNullOnInvalidInput(string? input)
    {
        Assert.Null(PlateDesignCodec.Decode(input));
    }

    [Fact]
    public void Decode_ReturnsNullOnWrongMagic()
    {
        var bytes = Convert.FromBase64String(PlateDesignCodec.Encode(Sample()));
        bytes[0] = (byte)'X';
        Assert.Null(PlateDesignCodec.Decode(Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void Decode_ReturnsNullOnPortraitPresetString()
    {
        // An HTPS portrait preset must not decode as a design code.
        var portrait = PortraitPresetCodec.Encode(new PortraitSnapshot());
        Assert.Null(PlateDesignCodec.Decode(portrait));
    }

    [Fact]
    public void Decode_ToleratesSurroundingWhitespace()
    {
        var code = "  " + PlateDesignCodec.Encode(Sample()) + "\n";
        Assert.NotNull(PlateDesignCodec.Decode(code));
    }
}
