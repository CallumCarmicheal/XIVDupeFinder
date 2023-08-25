using FFXIVClientStructs.FFXIV.Component.GUI;

using XIVDupeFinder;

using System;

namespace XIVDupeFinder.Inventories {
    internal class LargestInventory : CharacterInventory {
        public override string AddonName => "InventoryExpansion";
        public override int OffsetX => Plugin.Configuration.LargestInventoryOffset;

        protected override unsafe void InternalUpdateHighlights(bool forced = false) {
            if (_addon == IntPtr.Zero) { return; }

            for (int i = 0; i < 4; i++) {
                AtkUnitBase* grid = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("InventoryGrid" + i + "E", 1);
                UpdateGridHighlights(grid, 3, i);
            }
        }
    }
}
