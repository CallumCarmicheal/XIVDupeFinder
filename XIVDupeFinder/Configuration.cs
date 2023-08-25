using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace XIVDupeFinder
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool HighlightRandomColours { get; set; } = false;

        public bool HighlightDuplicates { get; set; } = true;
        public bool HightlightTabs { get; set; } = true;

        public byte[] TabHighlightColour  { get; set; } = new byte[3] { 80, 80, 80 };
        public byte[] ItemHighlightColour { get; set; } = new byte[3] { 80, 80, 80 };

        // offsets, taken from InventorySearchBar
        public int NormalInventoryOffset { get; set; } = 20;
        public int LargeInventoryOffset { get; set; } = 0;
        public int LargestInventoryOffset { get; set; } = 0;
        public int ChocoboInventoryOffset { get; set; } = 0;
        public int RetainerInventoryOffset { get; set; } = 18;
        public int LargeRetainerInventoryOffset { get; set; } = 0;
        public int ArmouryInventoryOffset { get; set; } = 30;

        #region Methods

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.PluginInterface = pluginInterface;
        }

        public void Save() {
            this.PluginInterface!.SavePluginConfig(this);
        }

        #endregion
    }
}
