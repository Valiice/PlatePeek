using PlatePeek.Core;

namespace PlatePeek;

/// <summary>Named plate snapshots saved as JSON under the plugin config folder.</summary>
public sealed class FavoritesService
{
    private static string Dir
    {
        get
        {
            var dir = Path.Combine(Services.PluginInterface.GetPluginConfigDirectory(), "favorites");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public IReadOnlyList<string> List()
        => Directory.GetFiles(Dir, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList()!;

    public void Save(string name, PlateSnapshot snapshot)
        => File.WriteAllText(PathFor(name), PlateSnapshotJson.Serialize(snapshot));

    public PlateSnapshot? Load(string name)
    {
        var path = PathFor(name);
        return File.Exists(path) ? PlateSnapshotJson.Deserialize(File.ReadAllText(path)) : null;
    }

    public void Delete(string name)
    {
        var path = PathFor(name);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static string PathFor(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(name.Trim().Where(c => Array.IndexOf(invalid, c) < 0));
        return Path.Combine(Dir, (safe.Length > 0 ? safe : "favorite") + ".json");
    }
}
