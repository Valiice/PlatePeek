using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PlatePeek.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration config;

    public ConfigWindow(Configuration config)
        : base("PlatePeek Settings###PlatePeekConfig")
    {
        this.config = config;
        this.Size = new System.Numerics.Vector2(360, 160);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var changed = false;

        var autoOpen = config.AutoOpen;
        if (ImGui.Checkbox("Auto-open inspector when viewing a plate", ref autoOpen))
        {
            config.AutoOpen = autoOpen;
            changed = true;
        }

        var ignoreOwn = config.IgnoreOwnPlate;
        if (ImGui.Checkbox("Don't auto-open for my own plate", ref ignoreOwn))
        {
            config.IgnoreOwnPlate = ignoreOwn;
            changed = true;
        }

        var debugDumps = config.DebugDumps;
        if (ImGui.Checkbox("Write debug dumps (dumps/latest.json)", ref debugDumps))
        {
            config.DebugDumps = debugDumps;
            changed = true;
        }

        if (changed)
            config.Save();
    }
}
