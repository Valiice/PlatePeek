namespace PlatePeek.Core;

/// <summary>
/// One shareable string for a whole look: the design code and the portrait
/// preset joined by '.', which never appears in base64. Decode also accepts a
/// bare design code or a bare portrait preset, so any PlatePeek or Portrait
/// Helper string pastes.
/// </summary>
public static class LookCodec
{
    public static string Encode(PlateSnapshot s)
        => PlateDesignCodec.Encode(DesignSnapshot.FromPlate(s)) + "." + PortraitPresetCodec.Encode(s.Portrait);

    public static (DesignSnapshot? Design, PortraitSnapshot? Portrait)? Decode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var parts = input.Trim().Split('.', 2);
        var design = PlateDesignCodec.Decode(parts[0]);
        var portrait = PortraitPresetCodec.Decode(parts.Length > 1 ? parts[1] : parts[0]);
        return design == null && portrait == null ? null : (design, portrait);
    }

    /// <summary>Builds a snapshot the inspector can show from a pasted code.</summary>
    public static PlateSnapshot ToSnapshot(DesignSnapshot? design, PortraitSnapshot? portrait) => new()
    {
        Name = "Pasted look",
        ContentId = ulong.MaxValue,
        BasePlate = design?.BasePlate ?? 0,
        TopBorder = design?.TopBorder ?? 0,
        BottomBorder = design?.BottomBorder ?? 0,
        InvertPortraitPlacement = design?.InvertPortraitPlacement ?? false,
        Decorations = design?.Decorations ?? [],
        Portrait = portrait ?? new PortraitSnapshot(),
        CapturedAt = DateTimeOffset.UtcNow,
    };
}
