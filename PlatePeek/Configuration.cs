using Dalamud.Configuration;

namespace PlatePeek;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool AutoOpen { get; set; } = true;
    public bool IgnoreOwnPlate { get; set; } = false;
    public bool DebugDumps { get; set; } = false;

    public void Save() => Services.PluginInterface.SavePluginConfig(this);
}
