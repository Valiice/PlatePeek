namespace PlatePeek.Core;

public enum PlateDecorationType : byte
{
    Invalid = 0,
    Backing = 1,
    PatternOverlay = 2,
    PortraitFrame = 3,
    PlateFrame = 4,
    Accent = 5,
}

public sealed record GearPiece
{
    public uint ItemId { get; init; }
    public byte Stain0 { get; init; }
    public byte Stain1 { get; init; }
}

/// <summary>Immutable copy of everything read from AgentCharaCard.Storage.</summary>
public sealed record PlateSnapshot
{
    // Identity / profile
    public string Name { get; init; } = "";
    public string WorldName { get; init; } = "";
    public ushort WorldId { get; init; }
    public byte ClassJobId { get; init; }
    public ushort Level { get; init; }
    public ushort TitleId { get; init; }
    public byte GrandCompany { get; init; }
    public byte GcRank { get; init; }
    public string FreeCompany { get; init; } = "";
    public string SearchComment { get; init; } = "";
    public byte PreferredClassJobId { get; init; }
    public IReadOnlyList<byte> PlayStyles { get; init; } = [];
    public uint ActiveHoursWeekdays { get; init; }
    public uint ActiveHoursWeekends { get; init; }

    // Plate design
    public ushort BasePlate { get; init; }
    public byte TopBorder { get; init; }
    public byte BottomBorder { get; init; }
    public bool InvertPortraitPlacement { get; init; }
    /// <summary>CharaCardDecoration row ids; type comes from that sheet's Component column.</summary>
    public IReadOnlyList<ushort> Decorations { get; init; } = [];

    // Portrait
    public PortraitSnapshot Portrait { get; init; } = new();

    // Portrait gear (14 slots; ItemId 0 = empty)
    public IReadOnlyList<GearPiece> Gear { get; init; } = [];
    public ushort Glasses0 { get; init; }
    public ushort Glasses1 { get; init; }
    public byte GearClassJobId { get; init; }

    // Meta
    public bool IsSelf { get; init; }
    public bool WasResetDueToFantasia { get; init; }
    public ulong ContentId { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
}
