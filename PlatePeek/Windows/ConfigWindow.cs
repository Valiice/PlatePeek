using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace PlatePeek.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration config;

    public ConfigWindow(Configuration config)
        : base("PlatePeek Settings###PlatePeekConfig", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        this.config = config;
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
        var dock = config.DockToPlate;
        using (ImRaii.Disabled(!config.AutoOpen))
        {
            if (ImGui.Checkbox("Don't auto-open for my own plate", ref ignoreOwn))
            {
                config.IgnoreOwnPlate = ignoreOwn;
                changed = true;
            }

            if (ImGui.Checkbox("Open next to the plate window", ref dock))
            {
                config.DockToPlate = dock;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Places the inspector to the right of the plate each time it auto-opens. You can still drag it afterwards.");
        }

        ImGui.Separator();

        var debugDumps = config.DebugDumps;
        if (ImGui.Checkbox("Write debug dumps", ref debugDumps))
        {
            config.DebugDumps = debugDumps;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Saves each captured plate as JSON in the plugin config folder (dumps/). Newest 20 are kept.");

        ImGui.Spacing();
        ImGui.TextDisabled("/platepeek show toggles the inspector.");

        if (changed)
            config.Save();
    }
}
