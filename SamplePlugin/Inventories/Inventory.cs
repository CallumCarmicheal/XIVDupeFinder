using CriticalCommonLib.Models;

using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Component.GUI;

using XIVDupeFinder;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace XIVDupeFinder.Inventories {
    public abstract unsafe class Inventory {
        public abstract string AddonName { get; }

        protected IntPtr _addon = IntPtr.Zero;
        public IntPtr Addon => _addon;

        public abstract int OffsetX { get; }

        protected AtkUnitBase* _node => (AtkUnitBase*)_addon;

        protected List<List<bool>>? _filter { get; set; } = null!;
        protected abstract List<List<bool>> GetEmptyFilter();

        protected abstract ulong CharacterId { get; }
        protected abstract InventoryCategory Category { get; }
        protected abstract int FirstBagOffset { get; }
        protected abstract int GridItemCount { get; }

        public bool IsVisible => _node != null && _node->IsVisible;
        public bool IsFocused() {
            if (_node == null || !_node->IsVisible) { return false; }
            if (_node->UldManager.NodeListCount < 2) { return false; }

            AtkComponentNode* window = _node->UldManager.NodeList[1]->GetAsAtkComponentNode();
            if (window == null || window->Component->UldManager.NodeListCount < 4) { return false; }

            return window->Component->UldManager.NodeList[3]->IsVisible;
        }

        public void UpdateAddonReference() {
            _addon = Plugin.GameGui.GetAddonByName(AddonName, 1);
        }

        public virtual void DiscoverDuplicates() {
            _filter = GetEmptyFilter();

            // Get items and group them by their Item Id
            List<InventoryItem> items = GetSortedItems();
            var groupedItems = items
                // Select items that are not fully stacked and can be stacked
                .Where  ( item => item.Item.StackSize > 1 && item.FullStack == false ) 
                .GroupBy( item => item.ItemId )
                .Select(group => new {
                    ItemId = group.Key,
                    Items = group.ToList(),
                    Count = group.Count()
                });

            foreach ( var itemGroups in groupedItems ) {
                try {
                    // Highlight if we have more then 1 item
                    bool highlight = itemGroups.Count > 1;

                    try {
                        foreach (InventoryItem item in itemGroups.Items) {
                            // map
                            int bagIndex = ContainerIndex(item) - FirstBagOffset;
                            if (_filter.Count > bagIndex) {
                                List<bool> bag = _filter[bagIndex];
                                int slot = GridItemCount - 1 - item.SortedSlotIndex;
                                if (bag.Count > slot) {
                                    bag[slot] = highlight;
                                }
                            }
                        }
                    }
                    //catch { }
                    catch (Exception e) {
                        PluginLog.Log(e.Message);
                    }
                }
                //catch { }
                catch (Exception e) {
                    PluginLog.Log(e.Message);
                }
            }
        }

        protected virtual List<InventoryItem> GetSortedItems() {
            return Plugin.InventoryMonitor.GetSpecificInventory(CharacterId, Category)
                .Where(item => item.ItemId != 0).ToList();
        }

        protected virtual int ContainerIndex(InventoryItem item) {
            return (int)item.SortedContainer;
        }

        public void UpdateHighlights() {
            InternalUpdateHighlights(false);
        }

        protected abstract void InternalUpdateHighlights(bool forced = false);

        public void ClearHighlights() {
            _filter = null;
            InternalUpdateHighlights(true);
        }

        protected unsafe void UpdateGridHighlights(AtkUnitBase* grid, int startIndex, int bagIndex) {
            if (grid == null) { return; }

            for (int j = startIndex; j < startIndex + GridItemCount; j++) {
                bool highlight = true;
                if (_filter != null && _filter[bagIndex].Count > j - startIndex) {
                    highlight = _filter[bagIndex][j - startIndex];
                }

                SetNodeHighlight(grid->UldManager.NodeList[j], highlight);
            }
        }

        protected static unsafe void SetNodeHighlight(AtkResNode* node, bool highlight) {
            node->MultiplyRed   = highlight || !node->IsVisible ? (byte)100 : (byte)20;
            node->MultiplyGreen = highlight || !node->IsVisible ? (byte)100 : (byte)20;
            node->MultiplyBlue  = highlight || !node->IsVisible ? (byte)100 : (byte)20;
        }

        public static unsafe void SetTabHighlight(AtkResNode* tab, bool highlight) {
            tab->MultiplyRed   = highlight ? (byte)250 : (byte)100;
            tab->MultiplyGreen = highlight ? (byte)250 : (byte)100;
            tab->MultiplyBlue  = highlight ? (byte)250 : (byte)100;
        }

        public static unsafe bool GetTabEnabled(AtkComponentBase* tab) {
            if (tab->UldManager.NodeListCount < 2) { return false; }

            return tab->UldManager.NodeList[2]->IsVisible;
        }

        public static unsafe bool GetSmallTabEnabled(AtkComponentBase* tab) {
            if (tab->UldManager.NodeListCount < 1) { return false; }

            return tab->UldManager.NodeList[1]->IsVisible;
        }
    }
}
