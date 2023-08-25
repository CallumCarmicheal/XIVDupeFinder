using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

using FFXIVClientStructs.FFXIV.Component.GUI;

using XIVDupeFinder;

using System;
using System.Collections.Generic;
using System.Linq;

namespace XIVDupeFinder.Inventories {
    internal class ChocoboInventory : Inventory {
        public override string AddonName => "InventoryBuddy";
        protected override ulong CharacterId => Plugin.CharacterMonitor.ActiveCharacterId;
        protected override InventoryCategory Category => InventoryCategory.CharacterSaddleBags;
        protected override int FirstBagOffset => (int)InventoryType.SaddleBag0;
        protected override int GridItemCount => 35;
        public override int OffsetX => Plugin.Configuration.ChocoboInventoryOffset;

        public ChocoboInventory() {

        }

        protected override List<List<HighlightItem>> GetEmptyFilter() {
            // 4 grids of 35 items
            List<List<HighlightItem>> emptyFilter = new List<List<HighlightItem>>();
            for (int i = 0; i < 4; i++) {
                List<HighlightItem> list = new List<HighlightItem>(GridItemCount);
                for (int j = 0; j < GridItemCount; j++) {
                    list.Add(new HighlightItem() { filtered = false, itemId = 0 });
                }

                emptyFilter.Add(list);
            }

            return emptyFilter;
        }

        protected override List<InventoryItem> GetSortedItems() {
            List<InventoryItem> list = new List<InventoryItem>();
            list.AddRange(Plugin.InventoryMonitor.GetSpecificInventory(CharacterId, InventoryCategory.CharacterSaddleBags));
            list.AddRange(Plugin.InventoryMonitor.GetSpecificInventory(CharacterId, InventoryCategory.CharacterPremiumSaddleBags));
            return list;
        }

        protected override int ContainerIndex(InventoryItem item) {
            if (item.SortedContainer >= InventoryType.PremiumSaddleBag0) {
                return (int)item.SortedContainer - 98;
            }

            return (int)item.SortedContainer;
        }

        protected override unsafe void InternalUpdateHighlights(bool forced = false) {
            if (_addon == IntPtr.Zero) { return; }

            int offset = GetGridOffset();

            UpdateGridHighlights(_node, 44, offset); // left grid
            UpdateGridHighlights(_node, 8, offset + 1); // right grid

            HighlightTabs(forced);
        }

        public unsafe void HighlightTabs(bool forced = false) {
            if (_node->UldManager.NodeListCount < 81) { return; }

            if (!Plugin.Configuration.HightlightTabs && !forced) { return; }

            AtkResNode* firstBagTab = _node->UldManager.NodeList[81];
            bool resultsInFirstTab = _filter != null && (_filter[0].Any(b => b.filtered == true) || _filter[1].Any(b => b.filtered == true));
            SetTabHighlight(firstBagTab, resultsInFirstTab);

            AtkResNode* secondBagTab = _node->UldManager.NodeList[80];
            bool resultsInSecondTab = _filter != null && (_filter[2].Any(b => b.filtered == true) || _filter[3].Any(b => b.filtered == true));
            SetTabHighlight(secondBagTab, resultsInSecondTab);
        }

        public unsafe int GetGridOffset() {
            if (_node->UldManager.NodeListCount < 80) { return 0; }

            AtkResNode* firstBagTab = _node->UldManager.NodeList[80];
            if (GetTabEnabled(firstBagTab->GetComponent())) {
                return 2;
            }

            return 0;
        }
    }

    internal class ChocoboInventory2 : ChocoboInventory {
        public override string AddonName => "InventoryBuddy2";
    }
}