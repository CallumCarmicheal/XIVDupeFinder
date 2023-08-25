using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace XIVDupeFinder.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base(
        "InvDupeFinder Config",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.Always;

        Configuration = Plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        bool boolValue;

        // can't ref a property, so use a local copy
        boolValue = this.Configuration.HighlightDuplicates;
        if (ImGui.Checkbox("Highlight Duplicates", ref boolValue)) {
            this.Configuration.HighlightDuplicates = boolValue;
            this.Configuration.Save();
        }

        boolValue = this.Configuration.HightlightTabs;
        if (ImGui.Checkbox("Highlight Inventory Tabs", ref boolValue)) {
            this.Configuration.HightlightTabs = boolValue;
            this.Configuration.Save();
        }
    }
}
