using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
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
    public DesignProbe Probe { get; }
    public FavoritesService Favorites { get; }

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

        Probe = new DesignProbe();
        DesignImport = new PlateDesignImportService(Unlocks);
        Inspector.IsDesignEditorOpen = DesignImport.IsDesignEditorOpen;
        Inspector.OnCopyLook = view =>
        {
            ImGui.SetClipboardText(LookCodec.Encode(view.Snapshot));
            Services.ChatGui.Print("[PlatePeek] Look code copied - design and portrait in one string.");
        };
        Inspector.OnImportDesign = view =>
        {
            var ok = DesignImport.TryImport(DesignSnapshot.FromPlate(view.Snapshot), out var message);
            Services.ChatGui.Print($"[PlatePeek] {view.Snapshot.Name}: {message}");
            Services.Log.Info($"[Export] design import {(ok ? "ok" : "failed")}: {message}");
        };
        Inspector.OnPasteLook = () =>
        {
            var decoded = LookCodec.Decode(ImGui.GetClipboardText());
            if (decoded == null)
            {
                Services.ChatGui.Print("[PlatePeek] Clipboard does not contain a look code, design code or portrait preset.");
                return;
            }
            Inspector.Present(Resolver.Resolve(LookCodec.ToSnapshot(decoded.Value.Design, decoded.Value.Portrait)));
            Services.ChatGui.Print("[PlatePeek] Pasted look loaded. Use the Import buttons to apply it.");
        };

        Inspector.OnTryOn = item =>
        {
            var ok = AgentTryon.TryOn(0, item.RowId, item.Stain0, item.Stain1);
            if (!ok)
                Services.ChatGui.Print("[PlatePeek] Could not open the fitting room for that item.");
        };

        Favorites = new FavoritesService();
        Inspector.ListFavorites = Favorites.List;
        Inspector.OnSaveFavorite = (name, view) =>
        {
            Favorites.Save(name, view.Snapshot);
            Services.ChatGui.Print($"[PlatePeek] Saved favorite \"{name}\".");
        };
        Inspector.OnLoadFavorite = name =>
        {
            var snapshot = Favorites.Load(name);
            if (snapshot == null)
            {
                Services.ChatGui.Print($"[PlatePeek] Favorite \"{name}\" could not be read.");
                return;
            }
            Inspector.Present(Resolver.Resolve(snapshot));
        };
        Inspector.OnDeleteFavorite = Favorites.Delete;

        Capture.SnapshotCaptured += snapshot =>
        {
            var resolved = Resolver.Resolve(snapshot);
            Inspector.SetSnapshot(resolved);
            if (Config.AutoOpen && !(Config.IgnoreOwnPlate && snapshot.IsSelf))
            {
                if (!Inspector.IsOpen && Config.DockToPlate)
                    Inspector.RequestDock();
                Inspector.IsOpen = true;
            }
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

        if (trimmed.Equals("probe", StringComparison.OrdinalIgnoreCase))
        {
            Probe.Toggle();
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
        Probe.Dispose();
        WindowSystem.RemoveAllWindows();
    }
}
