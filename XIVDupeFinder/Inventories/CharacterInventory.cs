using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

using XIVDupeFinder;

using System;
using System.Collections.Generic;

namespace XIVDupeFinder.Inventories {
    internal class CharacterInventory : Inventory {
        public override string AddonName => throw new NotImplementedException();
        protected override ulong CharacterId => Plugin.CharacterMonitor.ActiveCharacterId;
        protected override InventoryCategory Category => InventoryCategory.CharacterBags;
        protected override int FirstBagOffset => (int)InventoryType.Bag0;
        protected override int GridItemCount => 35;
        public override int OffsetX => 0;

        public CharacterInventory() {

        }

        protected override List<List<HighlightItem>> GetEmptyFilter() {
            // 4 grids of 35 items
            List<List<HighlightItem>> emptyFilter = new List<List<HighlightItem>>();
            for (int i = 0; i < 4; i++) {
                List<HighlightItem> list = new List<HighlightItem>(GridItemCount);
                for (int j = 0; j < GridItemCount; j++) {
                    list.Add(new() { filtered = false, itemId = 0 });
                }

                emptyFilter.Add(list);
            }

            return emptyFilter;
        }

        protected override void InternalUpdateHighlights(bool forced = false) {
            throw new NotImplementedException();
        }
    }
}
