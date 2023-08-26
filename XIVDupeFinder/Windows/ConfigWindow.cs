using System;
using System.Numerics;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace XIVDupeFinder.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    private int[] _tabHighlightColour = new int[3] { 0, 0, 0 };
    private int[] _itemHighlightColour = new int[3] { 0, 0, 0 };

    public ConfigWindow(Plugin plugin) : base(
        "XIVDupeFinder Config",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(440, 320);
        this.SizeCondition = ImGuiCond.Always;

        Configuration = Plugin.Configuration;

        _tabHighlightColour[0]  = Configuration.TabHighlightColour[0]; 
        _tabHighlightColour[1]  = Configuration.TabHighlightColour[1];
        _tabHighlightColour[2]  = Configuration.TabHighlightColour[2];
        _itemHighlightColour[0] = Configuration.ItemHighlightColour[0];
        _itemHighlightColour[1] = Configuration.ItemHighlightColour[1];
        _itemHighlightColour[2] = Configuration.ItemHighlightColour[2];
    }

    public void Dispose() { }

    public override void Draw()
    {
        bool boolValue;
        bool saveChanges = false;

        boolValue = this.Configuration.HighlightDuplicates;
        if (ImGui.Checkbox("Highlight Duplicates", ref boolValue)) {
            this.Configuration.HighlightDuplicates = boolValue;
            saveChanges = true;

            Plugin.ClearHighlights();
        }

        boolValue = this.Configuration.OnlyDuringKeyModifier;
        if (ImGui.Checkbox("* Only show when holding ALT (Modifier)", ref boolValue)) {
            this.Configuration.OnlyDuringKeyModifier = boolValue;
            saveChanges = true;

            Plugin.ClearHighlights();
        }


        boolValue = this.Configuration.HighlightOnlyActiveWindow;
        if (ImGui.Checkbox("Only active inventory window", ref boolValue)) {
            this.Configuration.HighlightOnlyActiveWindow = boolValue;
            saveChanges = true;

            Plugin.ClearHighlights();
        }

        boolValue = this.Configuration.HightlightTabs;
        if (ImGui.Checkbox("Highlight Inventory Tabs", ref boolValue)) {
            this.Configuration.HightlightTabs = boolValue;
            saveChanges = true;

            Plugin.ClearHighlights();
        }

        boolValue = this.Configuration.HighlightRandomColours;
        if (ImGui.Checkbox("Use Pastel Colour Pallete", ref boolValue)) {
            this.Configuration.HighlightRandomColours = boolValue;
            saveChanges = true;

            Plugin.ClearHighlights();
        }

        if (boolValue) ImGui.BeginDisabled();

        ImGui.DragInt3("Item Highlight Colour", ref _itemHighlightColour[0], 1, 10, 100);
        if (_itemHighlightColour[0] != Configuration.ItemHighlightColour[0]
                || _itemHighlightColour[1] != Configuration.ItemHighlightColour[1]
                || _itemHighlightColour[2] != Configuration.ItemHighlightColour[2]) {
            saveChanges = true;

            Configuration.ItemHighlightColour[0] = (byte)_itemHighlightColour[0];
            Configuration.ItemHighlightColour[1] = (byte)_itemHighlightColour[1];
            Configuration.ItemHighlightColour[2] = (byte)_itemHighlightColour[2];
        }

        if (boolValue) ImGui.EndDisabled();

        ImGui.DragInt3("Tab Highlight Colour", ref _tabHighlightColour[0], 1, 10, 100);
        if (_tabHighlightColour[0] != Configuration.TabHighlightColour[0]
                || _tabHighlightColour[1] != Configuration.TabHighlightColour[1]
                || _tabHighlightColour[2] != Configuration.TabHighlightColour[2]) {
            saveChanges = true;
            Configuration.TabHighlightColour[0] = (byte)_tabHighlightColour[0];
            Configuration.TabHighlightColour[1] = (byte)_tabHighlightColour[1];
            Configuration.TabHighlightColour[2] = (byte)_tabHighlightColour[2];
        }

        if (saveChanges) 
            this.Configuration.Save();

        ImGui.Text("Special thanks to Tischel for their work on InventorySearchBar.");
        ImGui.Text("Please enable the modified (holding ALT) if you experience any\nproblems with other plugins such as InventorySearchBar!");
        ImGui.Text("Modifier (ALT) Pressed: " + (Plugin.KeyState[VirtualKey.MENU] ? "True" : "False"));
        ImGui.Text("\n* Recommended Options");
    }
}
