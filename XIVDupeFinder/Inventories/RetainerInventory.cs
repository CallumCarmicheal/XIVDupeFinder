using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

using FFXIVClientStructs.FFXIV.Component.GUI;

using XIVDupeFinder;

using System;
using System.Collections.Generic;
using System.Linq;

namespace XIVDupeFinder.Inventories {
    internal class RetainerInventory : Inventory {
        public override string AddonName => "InventoryRetainer";
        protected override ulong CharacterId => Plugin.CharacterMonitor.ActiveRetainer.CharacterId;
        protected override InventoryCategory Category => InventoryCategory.RetainerBags;
        protected override int FirstBagOffset => (int)InventoryType.RetainerBag0;
        protected override int GridItemCount => 35;
        public override int OffsetX => Plugin.Configuration.RetainerInventoryOffset;

        protected int _tabCount = 5;
        protected int _tabIndexStart = 13;

        public RetainerInventory() {
            //
        }

        protected override List<List<HighlightItem>> GetEmptyFilter() {
            // 5 grids of 35 items
            List<List<HighlightItem>> emptyFilter = new List<List<HighlightItem>>();
            for (int i = 0; i < 5; i++) {
                List<HighlightItem> list = new List<HighlightItem>(GridItemCount);
                for (int j = 0; j < GridItemCount; j++) {
                    list.Add(new() { filtered = false, itemId = 0 });
                }

                emptyFilter.Add(list);
            }

            return emptyFilter;
        }

        protected override unsafe void InternalUpdateHighlights(bool forced = false) {
            if (_addon == IntPtr.Zero) { return; }

            int offset = GetGridOffset();
            if (offset == -1) { return; }

            AtkUnitBase* grid = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("RetainerGrid", 1);
            UpdateGridHighlights(grid, 3, offset);

            HighlightTabs(forced);
        }

        public unsafe void HighlightTabs(bool forced = false) {
            if (!Plugin.Configuration.HightlightTabs && !forced) { return; }

            for (int i = 0; i < _tabCount; i++) {
                UpdateTabHighlight(i);
            }
        }

        protected virtual unsafe void UpdateTabHighlight(int index) {
            if (_node == null || _node->UldManager.NodeListCount < _tabIndexStart) { return; }

            AtkResNode* tab = _node->UldManager.NodeList[_tabIndexStart - index];
            bool resultsInTab = _filter != null && _filter[index].Any(b => b.filtered == true);
            SetTabHighlight(tab, resultsInTab);
        }

        public virtual unsafe int GetGridOffset() {
            if (_node == null || _node->UldManager.NodeListCount < _tabIndexStart) { return -1; }

            for (int i = 0; i < _tabCount; i++) {
                AtkResNode* bagNode = _node->UldManager.NodeList[_tabIndexStart - i];
                if (GetSmallTabEnabled(bagNode->GetComponent())) {
                    return i;
                }
            }

            return -1;
        }
    }
}
