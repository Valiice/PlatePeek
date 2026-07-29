using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using PlatePeek.Core;

namespace PlatePeek.Windows;

public class InspectorWindow : Window
{
    private readonly CaptureRouter router = new();
    private float kofiButtonWidth;

    private ResolvedPlateView? view => router.View;

    public bool Pinned
    {
        get => router.Pinned;
        set => router.Pinned = value;
    }

    public Action<ResolvedPlateView>? OnCopyPreset;
    public Action<ResolvedPlateView>? OnCopyText;
    public Action<ResolvedPlateView>? OnImportPortrait;
    public Func<bool>? IsBannerEditorOpen;
    public Action<ResolvedPlateView>? OnCopyDesign;
    public Action<ResolvedPlateView>? OnImportDesign;
    public Action? OnPasteDesign;
    public Func<bool>? IsDesignEditorOpen;

    public InspectorWindow() : base("PlatePeek###PlatePeekInspector")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new System.Numerics.Vector2(380, 300),
        };
    }

    /// <summary>Stores the resolved view without opening the window. Routing
    /// rules (captured players keep the screen, own plate stashes, pin freezes)
    /// live in <see cref="CaptureRouter"/>.</summary>
    public void SetSnapshot(ResolvedPlateView newView) => router.Accept(newView);

    public void OnPlateClosed()
    {
        if (!Pinned)
            IsOpen = false;
    }

    public override void Draw()
    {
        if (view == null)
        {
            ImGui.TextUnformatted("No plate captured yet. Open someone's adventurer plate.");
            return;
        }

        var s = view.Snapshot;

        // Header
        ImGui.TextUnformatted($"{s.Name} @ {s.WorldName}");
        ImGui.SameLine();
        ImGui.TextDisabled($"Lv{s.Level} {view.JobName}" + (view.TitleName.Length > 0 ? $" <{view.TitleName}>" : ""));
        var style = ImGui.GetStyle();
        var pinWidth = ImGui.GetFrameHeight() + style.ItemInnerSpacing.X + ImGui.CalcTextSize("Pin").X;
        var clusterWidth = pinWidth + (kofiButtonWidth > 0 ? kofiButtonWidth + style.ItemSpacing.X : 0);
        ImGui.SameLine(Math.Max(ImGui.GetCursorPosX(), ImGui.GetContentRegionMax().X - clusterWidth));
        var pinned = Pinned;
        if (ImGui.Checkbox("Pin", ref pinned))
            Pinned = pinned;
        ImGui.SameLine();
        if (ImGuiComponents.IconButtonWithText(Dalamud.Interface.FontAwesomeIcon.Coffee, "Support",
            new System.Numerics.Vector4(1.0f, 0.35f, 0.35f, 0.9f),
            new System.Numerics.Vector4(1.0f, 0.25f, 0.25f, 1.0f),
            new System.Numerics.Vector4(1.0f, 0.35f, 0.35f, 0.75f)))
        {
            Dalamud.Utility.Util.OpenLink("https://ko-fi.com/valiice");
        }
        kofiButtonWidth = ImGui.GetItemRectSize().X;
        if (s.WasResetDueToFantasia)
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.7f, 0.2f, 1f), "Plate was reset by Fantasia.");

        if (router.CanSwitch && ImGui.SmallButton($"Switch to {router.Stashed!.Snapshot.Name}"))
            router.Switch();

        DrawActionButtons();
        ImGui.Separator();

        if (ImGui.BeginTabBar("PlatePeekTabs"))
        {
            if (ImGui.BeginTabItem("Design"))
            {
                DrawItemTable("design", view.DesignItems);
                ImGui.Spacing();
                ImGui.TextUnformatted($"Invert Portrait Placement: {(s.InvertPortraitPlacement ? "Yes" : "No")}");
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Portrait"))
            {
                DrawItemTable("portrait", view.PortraitItems);
                ImGui.Spacing();
                ImGui.TextUnformatted($"Pose frame: {s.Portrait.AnimationProgress.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
                ImGui.TextUnformatted($"Expression: {view.ExpressionName}");
                ImGui.TextUnformatted($"Camera zoom: {s.Portrait.CameraZoom}   Rotation: {s.Portrait.ImageRotation}");
                ImGui.TextUnformatted(
                    $"Directional light: RGB({s.Portrait.DirectionalLightingColorRed}, {s.Portrait.DirectionalLightingColorGreen}, {s.Portrait.DirectionalLightingColorBlue}) " +
                    $"{s.Portrait.DirectionalLightingBrightness}%  V{s.Portrait.DirectionalLightingVerticalAngle}° H{s.Portrait.DirectionalLightingHorizontalAngle}°");
                ImGui.TextUnformatted(
                    $"Ambient light: RGB({s.Portrait.AmbientLightingColorRed}, {s.Portrait.AmbientLightingColorGreen}, {s.Portrait.AmbientLightingColorBlue}) " +
                    $"{s.Portrait.AmbientLightingBrightness}%");
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.TextUnformatted("Gear");
                DrawItemTable("gear", view.GearItems, showCategory: false);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Profile"))
            {
                ImGui.TextUnformatted($"Favored job: {view.PreferredJobName}");
                if (view.GrandCompanyName.Length > 0)
                    ImGui.TextUnformatted($"Grand Company: {view.GrandCompanyName} (rank {s.GcRank})");
                if (s.FreeCompany.Length > 0)
                    ImGui.TextUnformatted($"Free Company: {s.FreeCompany}");
                ImGui.TextUnformatted($"Playstyles: {string.Join(", ", view.PlayStyleNames)}");
                DrawActiveHours("Weekdays", s.ActiveHoursWeekdays);
                DrawActiveHours("Weekends", s.ActiveHoursWeekends);
                if (s.SearchComment.Length > 0)
                {
                    ImGui.Separator();
                    ImGui.TextUnformatted("Comment");
                    ImGui.TextWrapped(s.SearchComment);
                }
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawActionButtons()
    {
        if (view == null)
            return;

        if (ImGui.Button("Copy portrait preset"))
            OnCopyPreset?.Invoke(view);
        ImGui.SameLine();
        var editorOpen = IsBannerEditorOpen?.Invoke() ?? false;
        using (ImRaii.Disabled(!editorOpen))
        {
            if (ImGui.Button("Import into my portrait editor"))
                OnImportPortrait?.Invoke(view);
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(editorOpen
                ? $"Applies {view.Snapshot.Name}'s captured portrait to your open editor."
                : "Open your own portrait editor first:\nyour plate > Edit Plate > Portrait > Edit (plate portrait),\nor Portraits list > Edit (gearset portrait).\nThe import lands in whichever editor you opened; hit Save after.");
        ImGui.SameLine();
        if (ImGui.Button("Copy as text"))
            OnCopyText?.Invoke(view);

        if (ImGui.Button("Copy design code"))
            OnCopyDesign?.Invoke(view);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Copies the plate design layers as a code other PlatePeek users can import.");
        ImGui.SameLine();
        var designEditorOpen = IsDesignEditorOpen?.Invoke() ?? false;
        using (ImRaii.Disabled(!designEditorOpen))
        {
            if (ImGui.Button("Import design"))
                OnImportDesign?.Invoke(view);
            ImGui.SameLine();
            if (ImGui.Button("Paste design code"))
                OnPasteDesign?.Invoke();
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(designEditorOpen
                ? "Import design applies this capture; Paste design code applies a code from your clipboard.\nCheck the preview and hit Save after."
                : "Open your plate design editor first: your plate > Edit Plate > Edit Design.");
    }

    private static void DrawItemTable(string id, IReadOnlyList<ResolvedItem> items, bool showCategory = true)
    {
        if (!ImGui.BeginTable($"###platePeek_{id}", showCategory ? 3 : 1, ImGuiTableFlags.SizingStretchProp))
            return;

        foreach (var item in items)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (item.IconId != 0)
            {
                var wrap = Services.TextureProvider.GetFromGameIcon(new GameIconLookup(item.IconId)).GetWrapOrEmpty();
                ImGui.Image(wrap.Handle, new System.Numerics.Vector2(24, 24));
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
            }

            if (!showCategory)
            {
                ImGui.TextUnformatted(item.Name);
                continue;
            }

            ImGui.TextUnformatted(item.Category);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(item.Name);
            ImGui.TableNextColumn();
            DrawStatusCell(item);
        }

        ImGui.EndTable();
    }

    private static void DrawStatusCell(ResolvedItem item)
    {
        if (item.Unlocked == true)
        {
            ImGui.TextColored(new System.Numerics.Vector4(0.3f, 1f, 0.3f, 1f), "Owned");
            return;
        }

        if (item.Unlocked == false)
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.4f, 0.4f, 1f), "Missing");
        else
            ImGui.TextDisabled("?");

        var clickable = item.LinkUrl is { Length: > 0 };
        if (ImGui.IsItemHovered())
        {
            var lines = new List<string>();
            if (item.Unlocked == null)
                lines.Add("Ownership can't be checked right now.");
            if (item.UnlockHint is { Length: > 0 } hint)
                lines.Add($"Source: {hint}");
            if (clickable)
                lines.Add("Click to look up in your browser.");
            if (lines.Count > 0)
                ImGui.SetTooltip(string.Join("\n", lines));
        }

        if (clickable && ImGui.IsItemClicked())
            Dalamud.Utility.Util.OpenLink(item.LinkUrl!);
    }

    private static void DrawActiveHours(string label, uint bitmask)
    {
        // 24 bits, one per hour slot as shown in the game's profile editor.
        var active = new List<int>();
        for (var hour = 0; hour < 24; hour++)
        {
            if ((bitmask & (1u << hour)) != 0)
                active.Add(hour);
        }
        ImGui.TextUnformatted(active.Count == 0
            ? $"{label}: none set"
            : $"{label}: {string.Join(", ", active.Select(h => $"{h:00}:00"))}");
    }
}
