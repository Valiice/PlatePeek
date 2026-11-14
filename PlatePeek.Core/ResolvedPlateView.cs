namespace PlatePeek.Core;

public sealed record ResolvedItem(
    string Category,
    uint RowId,
    string Name,
    bool? Unlocked,
    string? UnlockHint,
    uint IconId,
    string? LinkUrl = null,
    byte Stain0 = 0,
    byte Stain1 = 0);

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
