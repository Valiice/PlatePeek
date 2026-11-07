using System.Text;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PlatePeek;

/// <summary>
/// Temporary diagnostic for discovering the Edit Plate Design addon's
/// callback protocol. Toggled with "/platepeek probe". While enabled it logs
/// every callback the plate windows fire into the game and every UI event
/// they receive, so a manual click-through reveals which values select each
/// design layer. Not part of the shipped feature set.
/// </summary>
public sealed unsafe class DesignProbe : IDisposable
{
    private static readonly string[] Addons = ["CharaCardDesignSetting", "CharaCardEditMenu", "CharaCard"];

    private delegate bool FireCallbackDelegate(AtkUnitBase* addon, uint valueCount, AtkValue* values, bool close);

    private Hook<FireCallbackDelegate>? fireCallbackHook;

    public bool Enabled { get; private set; }

    public void Toggle()
    {
        if (Enabled)
            Disable();
        else
            Enable();
    }

    private void Enable()
    {
        fireCallbackHook ??= Services.GameInterop.HookFromAddress<FireCallbackDelegate>(
            (nint)AtkUnitBase.MemberFunctionPointers.FireCallback, OnFireCallback);
        fireCallbackHook.Enable();
        foreach (var addon in Addons)
            Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, addon, OnReceiveEvent);
        Enabled = true;
        Services.Log.Info("[Probe] enabled");
        Services.ChatGui.Print("[PlatePeek] Probe ON. Open Edit Plate, change each design dropdown by one step, toggle Invert, then run /platepeek probe again to stop.");
    }

    private void Disable()
    {
        fireCallbackHook?.Disable();
        Services.AddonLifecycle.UnregisterListener(OnReceiveEvent);
        Enabled = false;
        Services.Log.Info("[Probe] disabled");
        Services.ChatGui.Print("[PlatePeek] Probe OFF.");
    }

    private bool OnFireCallback(AtkUnitBase* addon, uint valueCount, AtkValue* values, bool close)
    {
        try
        {
            var name = addon->NameString;
            if (Array.IndexOf(Addons, name) >= 0)
                Services.Log.Info($"[Probe] FireCallback {name} close={close} values=[{FormatValues(valueCount, values)}]");
        }
        catch
        {
            // Never let diagnostics break the UI path.
        }

        return fireCallbackHook!.Original(addon, valueCount, values, close);
    }

    private void OnReceiveEvent(AddonEvent type, AddonArgs args)
    {
        try
        {
            if (args is AddonReceiveEventArgs e)
                Services.Log.Info($"[Probe] ReceiveEvent {args.AddonName} type={e.AtkEventType} param={e.EventParam}");
        }
        catch
        {
        }
    }

    private static string FormatValues(uint count, AtkValue* values)
    {
        if (values == null)
            return "";

        var sb = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            var v = values[i];
            sb.Append(v.Type switch
            {
                AtkValueType.Int => $"int:{v.Int}",
                AtkValueType.UInt => $"uint:{v.UInt}",
                AtkValueType.Bool => $"bool:{v.Byte != 0}",
                AtkValueType.Float => $"float:{v.Float}",
                AtkValueType.String or AtkValueType.ManagedString or AtkValueType.ConstString => $"str:{v.String}",
                _ => $"{v.Type}:0x{v.Int:X}",
            });
        }

        return sb.ToString();
    }

    public void Dispose()
    {
        if (Enabled)
            Disable();
        fireCallbackHook?.Dispose();
    }
}
