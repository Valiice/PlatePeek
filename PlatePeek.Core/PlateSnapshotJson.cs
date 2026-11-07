using System.Text.Json;

namespace PlatePeek.Core;

/// <summary>Shared JSON settings for favorites and debug dumps.</summary>
public static class PlateSnapshotJson
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Serialize(PlateSnapshot s) => JsonSerializer.Serialize(s, Options);

    public static PlateSnapshot? Deserialize(string json) => JsonSerializer.Deserialize<PlateSnapshot>(json, Options);
}
