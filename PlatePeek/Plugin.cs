using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using PlatePeek.Core;
using PlatePeek.Windows;

namespace PlatePeek;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/platepeek";

    public readonly WindowSystem WindowSystem = new("PlatePeek");
    public Configuration Config { get; }
    public CaptureService Capture { get; }
    public UnlockService Unlocks { get; }
    public SheetResolver Resolver { get; }
    public InspectorWindow Inspector { get; }
    public DiagnosticsService Diagnostics { get; }
    public PortraitImportService PortraitImport { get; }
    public PlateDesignImportService DesignImport { get; }

    private readonly ConfigWindow configWindow;

    /// <summary>Set by DiagnosticsService (Task 8); invoked by "/platepeek selftest".</summary>
    public Action? SelfTestRequested { get; set; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();

        Config = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        configWindow = new ConfigWindow(Config);
        WindowSystem.AddWindow(configWindow);

        Capture = new CaptureService();
        Unlocks = new UnlockService();
        Resolver = new SheetResolver(Unlocks);

        Inspector = new InspectorWindow();
        WindowSystem.AddWindow(Inspector);

        Diagnostics = new DiagnosticsService(Config, Capture, Resolver);
        SelfTestRequested = Diagnostics.RunSelfTest;

        PortraitImport = new PortraitImportService(Unlocks);

        Inspector.IsBannerEditorOpen = PortraitImport.IsEditorOpen;
        Inspector.OnCopyPreset = view =>
        {
            var preset = PortraitPresetCodec.Encode(view.Snapshot.Portrait);
            ImGui.SetClipboardText(preset);
            Services.ChatGui.Print("[PlatePeek] Portrait preset copied - paste in Portrait Helper or import directly.");
            Services.Log.Info($"[Export] preset copied for {view.Snapshot.Name}: {preset}");
        };
        Inspector.OnCopyText = view =>
        {
            ImGui.SetClipboardText(PlateTextSummary.Format(view));
            Services.ChatGui.Print("[PlatePeek] Plate design copied as text.");
        };
        Inspector.OnImportPortrait = view =>
        {
            try
            {
                var ok = PortraitImport.TryImport(view.Snapshot.Portrait, out var message);
                Services.ChatGui.Print($"[PlatePeek] {view.Snapshot.Name}: {message}");
                Services.Log.Info($"[Export] direct import {(ok ? "ok" : "failed")}: {message}");
            }
            catch (Exception ex)
            {
                Services.Log.Error(ex, "[Export] direct import threw");
                Services.ChatGui.Print("[PlatePeek] Import failed - see /xllog.");
            }
        };

        DesignImport = new PlateDesignImportService(Unlocks);
        Inspector.IsDesignEditorOpen = DesignImport.IsDesignEditorOpen;
        Inspector.OnCopyDesign = view =>
        {
            ImGui.SetClipboardText(PlateDesignCodec.Encode(DesignSnapshot.FromPlate(view.Snapshot)));
            Services.ChatGui.Print("[PlatePeek] Design code copied.");
        };
        Inspector.OnImportDesign = view =>
        {
            var ok = DesignImport.TryImport(DesignSnapshot.FromPlate(view.Snapshot), out var message);
            Services.ChatGui.Print($"[PlatePeek] {view.Snapshot.Name}: {message}");
            Services.Log.Info($"[Export] design import {(ok ? "ok" : "failed")}: {message}");
        };
        Inspector.OnPasteDesign = () =>
        {
            var design = PlateDesignCodec.Decode(ImGui.GetClipboardText());
            if (design == null)
            {
                Services.ChatGui.Print("[PlatePeek] Clipboard does not contain a valid design code.");
                return;
            }
            var ok = DesignImport.TryImport(design, out var message);
            Services.ChatGui.Print($"[PlatePeek] {message}");
            Services.Log.Info($"[Export] design paste {(ok ? "ok" : "failed")}: {message}");
        };

        Capture.SnapshotCaptured += snapshot =>
        {
            var resolved = Resolver.Resolve(snapshot);
            Inspector.SetSnapshot(resolved);
            if (Config.AutoOpen && !(Config.IgnoreOwnPlate && snapshot.IsSelf))
                Inspector.IsOpen = true;
        };
        Capture.PlateClosed += Inspector.OnPlateClosed;

        Unlocks.CacheUpdated += () =>
        {
            if (Capture.Latest is { } latest)
                Inspector.SetSnapshot(Resolver.Resolve(latest));
        };

        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open PlatePeek settings. \"/platepeek show\" toggles the inspector. \"/platepeek selftest\" runs diagnostics.",
        });

        Services.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        Services.PluginInterface.UiBuilder.OpenMainUi += OnOpenMainUi;
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim();
        if (trimmed.Equals("selftest", StringComparison.OrdinalIgnoreCase))
        {
            SelfTestRequested?.Invoke();
            return;
        }

        if (trimmed.Equals("show", StringComparison.OrdinalIgnoreCase))
        {
            Inspector.Toggle();
            return;
        }

        configWindow.Toggle();
    }

    private void OnOpenConfigUi() => configWindow.Toggle();

    private void OnOpenMainUi() => Inspector.Toggle();

    public void Dispose()
    {
        Services.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        Services.PluginInterface.UiBuilder.OpenMainUi -= OnOpenMainUi;
        Services.CommandManager.RemoveHandler(CommandName);
        Capture.Dispose();
        Unlocks.Dispose();
        WindowSystem.RemoveAllWindows();
    }
}
