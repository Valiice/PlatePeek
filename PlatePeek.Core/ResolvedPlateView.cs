namespace PlatePeek.Core;

/// <summary>One display row: a category label, the raw row id, resolved name,
/// unlock state for the local player (null = not applicable), an optional
/// unlock hint, and a game icon id (0 = none).</summary>
public sealed record ResolvedItem(
    string Category,
    uint RowId,
    string Name,
    bool? Unlocked,
    string? UnlockHint,
    uint IconId,
    string? LinkUrl = null);

/// <summary>Everything the UI, text summary, and JSON dump need: snapshot
/// plus all names resolved. Built by the plugin, consumed by Core.</summary>
public sealed record ResolvedPlateView
{
    public PlateSnapshot Snapshot { get; init; } = new();
    public string JobName { get; init; } = "";
    public string TitleName { get; init; } = "";
    public string PreferredJobName { get; init; } = "";
    public string GrandCompanyName { get; init; } = "";
    public string PoseName { get; init; } = "";
    public string ExpressionName { get; init; } = "";
    public IReadOnlyList<ResolvedItem> DesignItems { get; init; } = [];
    public IReadOnlyList<ResolvedItem> PortraitItems { get; init; } = [];
    public IReadOnlyList<ResolvedItem> GearItems { get; init; } = [];
    public IReadOnlyList<string> PlayStyleNames { get; init; } = [];
}
