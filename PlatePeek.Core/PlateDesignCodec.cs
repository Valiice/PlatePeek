namespace PlatePeek.Core;

/// <summary>The plate design layers as a standalone, shareable unit.</summary>
public sealed record DesignSnapshot
{
    public ushort BasePlate { get; init; }
    public byte TopBorder { get; init; }
    public byte BottomBorder { get; init; }
    public bool InvertPortraitPlacement { get; init; }
    public IReadOnlyList<ushort> Decorations { get; init; } = [];

    public static DesignSnapshot FromPlate(PlateSnapshot s) => new()
    {
        BasePlate = s.BasePlate,
        TopBorder = s.TopBorder,
        BottomBorder = s.BottomBorder,
        InvertPortraitPlacement = s.InvertPortraitPlacement,
        Decorations = s.Decorations,
    };
}

/// <summary>
/// Reads and writes PlatePeek design code strings (magic "PPDS", version 1).
/// Layout is little-endian: int32 magic, ushort version, ushort base plate,
/// byte top border, byte bottom border, byte invert flag, byte decoration
/// count, then count x ushort decoration row ids. Base64 encoded.
/// </summary>
public static class PlateDesignCodec
{
    private const int Magic = 0x53445050; // "PPDS"
    private const ushort Version = 1;

    public static string Encode(DesignSnapshot d)
    {
        using var stream = new MemoryStream(32);
        using var writer = new BinaryWriter(stream);

        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(d.BasePlate);
        writer.Write(d.TopBorder);
        writer.Write(d.BottomBorder);
        writer.Write(d.InvertPortraitPlacement);
        writer.Write((byte)Math.Min(d.Decorations.Count, 5));
        foreach (var deco in d.Decorations.Take(5))
            writer.Write(deco);
        writer.Flush();

        return Convert.ToBase64String(stream.ToArray());
    }

    public static DesignSnapshot? Decode(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        byte[] raw;
        try
        {
            raw = Convert.FromBase64String(input.Trim());
        }
        catch (FormatException)
        {
            return null;
        }

        if (raw.Length < 12)
            return null;

        using var stream = new MemoryStream(raw);
        using var reader = new BinaryReader(stream);

        if (reader.ReadInt32() != Magic)
            return null;
        if (reader.ReadUInt16() != Version)
            return null;

        var basePlate = reader.ReadUInt16();
        var top = reader.ReadByte();
        var bottom = reader.ReadByte();
        var invert = reader.ReadBoolean();
        var count = reader.ReadByte();
        if (count > 5 || stream.Length - stream.Position < count * 2)
            return null;

        var decorations = new List<ushort>();
        for (var i = 0; i < count; i++)
            decorations.Add(reader.ReadUInt16());

        return new DesignSnapshot
        {
            BasePlate = basePlate,
            TopBorder = top,
            BottomBorder = bottom,
            InvertPortraitPlacement = invert,
            Decorations = decorations,
        };
    }
}
