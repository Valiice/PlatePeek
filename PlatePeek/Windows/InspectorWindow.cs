using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
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
    private bool dockPending;
    private string favoriteName = "";

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
    public Action<ResolvedPlateView>? OnCopyLook;
    public Action? OnPasteLook;
    public Action<ResolvedPlateView>? OnImportDesign;
    public Func<bool>? IsDesignEditorOpen;
    public Action<ResolvedItem>? OnTryOn;
    public Func<IReadOnlyList<string>>? ListFavorites;
    public Action<string, ResolvedPlateView>? OnSaveFavorite;
    public Action<string>? OnLoadFavorite;
    public Action<string>? OnDeleteFavorite;

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

    /// <summary>Puts a loaded favorite or pasted code on screen and opens the window,
    /// bypassing the own-plate stash rule.</summary>
    public void Present(ResolvedPlateView newView)
    {
        router.Accept(newView);
        router.Show(newView);
        IsOpen = true;
    }

    /// <summary>Next frame, place the window to the right of the plate window.</summary>
    public void RequestDock() => dockPending = true;

    public void OnPlateClosed()
    {
        if (!Pinned)
            IsOpen = false;
    }

    public override void PreDraw()
    {
        if (!dockPending)
            return;

        var addon = Services.GameGui.GetAddonByName("CharaCard");
        if (!addon.IsNull && addon.IsVisible)
        {
            Position = new System.Numerics.Vector2(addon.X + addon.ScaledWidth + 8, addon.Y);
            PositionCondition = ImGuiCond.Always;
        }
    }

    public override void PostDraw()
    {
        if (!dockPending)
            return;
        dockPending = false;
        Position = null;
    }

    public override void Draw()
    {
        if (view == null)
        {
            ImGui.TextUnformatted("No plate captured yet. Open someone's adventurer plate.");
            DrawFavorites();
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

        DrawRecentAndFavorites();
        DrawActionButtons();
        ImGui.Separator();

        if (ImGui.BeginTabBar("PlatePeekTabs"))
        {
            if (ImGui.BeginTabItem("Design"))
            {
                DrawOwnershipSummary(view.DesignItems, "design pieces");
                DrawItemTable("design", view.DesignItems);
                ImGui.Spacing();
                ImGui.TextUnformatted($"Invert Portrait Placement: {(s.InvertPortraitPlacement ? "Yes" : "No")}");
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Portrait"))
            {
                DrawOwnershipSummary(view.PortraitItems, "portrait pieces");
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
                ImGui.TextWrapped($"Playstyles: {string.Join(", ", view.PlayStyleNames)}");
                DrawActiveHours("Weekdays", s.ActiveHoursWeekdays);
                DrawActiveHours("Weekends", s.ActiveHoursWeekends);
                if (s.SearchComment.Length > 0)
                {
                    ImGui.Separator();
                    ImGui.TextUnformatted("Comment");
                    ImGui.TextWrapped(s.SearchComment);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        ImGui.SetTooltip("Click to copy.");
                        if (ImGui.IsItemClicked())
                        {
                            ImGui.SetClipboardText(s.SearchComment);
                            Services.ChatGui.Print("[PlatePeek] Search comment copied.");
                        }
                    }
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

        if (ImGui.Button("Copy look code"))
            OnCopyLook?.Invoke(view);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Copies design and portrait as one code. Other PlatePeek users paste it to see and import this look.");
        ImGui.SameLine();
        if (ImGui.Button("Paste code"))
            OnPasteLook?.Invoke();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Loads a look code, design code or Portrait Helper preset from your clipboard into this window.");
        ImGui.SameLine();
        var designEditorOpen = IsDesignEditorOpen?.Invoke() ?? false;
        using (ImRaii.Disabled(!designEditorOpen))
        {
            if (ImGui.Button("Import design"))
                OnImportDesign?.Invoke(view);
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(designEditorOpen
                ? "Applies this design to your open editor. Check the preview and hit Save after."
                : "Open your plate design editor first: your plate > Edit Plate > Edit Design.");
    }

    private void DrawRecentAndFavorites()
    {
        if (router.History.Count > 1)
        {
            ImGui.SetNextItemWidth(170);
            if (ImGui.BeginCombo("##recent", "Recent plates"))
            {
                foreach (var entry in router.History)
                {
                    if (ImGui.Selectable($"{entry.Snapshot.Name}##{entry.Snapshot.ContentId}", entry == view))
                        router.Show(entry);
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
        }

        DrawFavorites();
    }

    private void DrawFavorites()
    {
        if (view != null)
        {
            if (ImGui.Button("Save favorite"))
            {
                favoriteName = view.Snapshot.Name;
                ImGui.OpenPopup("PlatePeekSaveFavorite");
            }
            if (ImGui.BeginPopup("PlatePeekSaveFavorite"))
            {
                ImGui.SetNextItemWidth(220);
                var enter = ImGui.InputText("##favName", ref favoriteName, 64, ImGuiInputTextFlags.EnterReturnsTrue);
                ImGui.SameLine();
                if ((ImGui.Button("Save") || enter) && favoriteName.Trim().Length > 0)
                {
                    OnSaveFavorite?.Invoke(favoriteName.Trim(), view);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            ImGui.SameLine();
        }

        var favorites = ListFavorites?.Invoke() ?? [];
        using (ImRaii.Disabled(favorites.Count == 0))
        {
            ImGui.SetNextItemWidth(170);
            if (ImGui.BeginCombo("##favorites", favorites.Count == 0 ? "No favorites yet" : "Favorites"))
            {
                foreach (var name in favorites)
                {
                    if (ImGui.Selectable($"{name}##fav", false, ImGuiSelectableFlags.None, new System.Numerics.Vector2(200, 0)))
                        OnLoadFavorite?.Invoke(name);
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton($"##del_{name}", FontAwesomeIcon.Trash))
                        OnDeleteFavorite?.Invoke(name);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Delete this favorite");
                }
                ImGui.EndCombo();
            }
        }
    }

    /// <summary>"You own 7 of 9 design pieces. Missing: ..." for someone else's plate.</summary>
    private void DrawOwnershipSummary(IReadOnlyList<ResolvedItem> items, string what)
    {
        if (view == null || view.Snapshot.IsSelf)
            return;

        var known = items.Where(i => i.Unlocked != null).ToList();
        if (known.Count == 0)
            return;

        var missing = known.Where(i => i.Unlocked == false).Select(i => i.Name).ToList();
        var unchecked_ = items.Count - known.Count;
        var suffix = unchecked_ > 0 ? $" ({unchecked_} unchecked)" : "";
        if (missing.Count == 0)
            ImGui.TextColored(new System.Numerics.Vector4(0.3f, 1f, 0.3f, 1f), $"You own all {known.Count} {what}.{suffix}");
        else
            ImGui.TextWrapped($"You own {known.Count - missing.Count} of {known.Count} {what}{suffix}. Missing: {string.Join(", ", missing)}");
        ImGui.Spacing();
    }

    private void DrawItemTable(string id, IReadOnlyList<ResolvedItem> items, bool showCategory = true)
    {
        // Icon gets its own fixed column so wide plate textures don't push the text around.
        // Last column is the unlock status, or a "Try on" button for gear.
        if (!ImGui.BeginTable($"###platePeek_{id}", showCategory ? 4 : 3, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("icon", ImGuiTableColumnFlags.WidthFixed);
        if (showCategory)
            ImGui.TableSetupColumn("category", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("status", ImGuiTableColumnFlags.WidthFixed);

        foreach (var item in items)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawIcon(item.IconId);

            if (showCategory)
            {
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(item.Category);
            }

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            DrawNameLink(item);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (showCategory)
            {
                DrawStatusCell(item);
            }
            else if (item.Category == "Item" && OnTryOn != null)
            {
                if (ImGui.SmallButton($"Try on##{item.RowId}"))
                    OnTryOn(item);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Preview this piece (with its dyes) in the fitting room.");
            }
        }

        ImGui.EndTable();
    }

    /// <summary>24px-high thumbnail keeping the texture's aspect ratio (plate layers are
    /// wide strips, not square icons). Hovering a non-square one shows it full size.</summary>
    private static void DrawIcon(uint iconId)
    {
        if (iconId == 0)
            return;

        var wrap = Services.TextureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrEmpty();
        if (wrap.Height == 0)
            return;

        const float height = 24f;
        var width = Math.Min(height * wrap.Width / wrap.Height, 96f);
        ImGui.Image(wrap.Handle, new System.Numerics.Vector2(width, height));

        if (wrap.Width != wrap.Height && ImGui.IsItemHovered())
        {
            var scale = Math.Min(1f, 480f / wrap.Width);
            ImGui.BeginTooltip();
            ImGui.Image(wrap.Handle, new System.Numerics.Vector2(wrap.Width * scale, wrap.Height * scale));
            ImGui.EndTooltip();
        }
    }

    private static void DrawNameLink(ResolvedItem item)
    {
        ImGui.TextUnformatted(item.Name);
        if (item.LinkUrl is not { Length: > 0 } url || !ImGui.IsItemHovered())
            return;

        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        ImGui.SetTooltip("Click to look up in your browser.");
        if (ImGui.IsItemClicked())
            Dalamud.Utility.Util.OpenLink(url);
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

        if (ImGui.IsItemHovered())
        {
            var lines = new List<string>();
            if (item.Unlocked == null)
                lines.Add("Ownership can't be checked right now.");
            if (item.UnlockHint is { Length: > 0 } hint)
                lines.Add($"Source: {hint}");
            if (lines.Count > 0)
                ImGui.SetTooltip(string.Join("\n", lines));
        }
    }

    private static void DrawActiveHours(string label, uint bitmask)
        => ImGui.TextWrapped($"{label}: {PlateTextSummary.FormatActiveHours(bitmask)}");
}
