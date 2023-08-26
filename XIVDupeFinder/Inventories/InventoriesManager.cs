using Dalamud.Game.ClientState.Keys;

using System;
using System.Collections.Generic;
using System.Linq;

namespace XIVDupeFinder.Inventories {
    internal class InventoriesManager : IDisposable {
        private List<Inventory> _inventories;
        public bool ChangedActiveInventory { get; private set; } = false;
        public Inventory? LastInventory { get; private set; } = null!;
        public Inventory? ActiveInventory { get; private set; } = null!;
        public IEnumerable<Inventory?> OpenInventories { get; private set; } = null!;


        public InventoriesManager() {
            _inventories = new List<Inventory>()
            {
                new NormalInventory(),
                new LargeInventory(),
                new LargestInventory(),
                new ChocoboInventory(),
                new ChocoboInventory2(),
                new RetainerInventory(),
                new LargeRetainerInventory(),
                new ArmouryInventory()
            };
        }

        public void Update() {
            foreach (Inventory inventory in _inventories) {
                inventory.UpdateAddonReference();
            }

            // Apply our highlighting
            bool highlightEnabled = Plugin.Configuration.OnlyDuringKeyModifier
                   ? Plugin.KeyState[VirtualKey.MENU]
                   : Plugin.Configuration.HighlightDuplicates;

            if (highlightEnabled) {
                LastInventory = ActiveInventory;
                ActiveInventory = _inventories.FirstOrDefault(o => o.IsVisible && o.IsFocused());
                ChangedActiveInventory = LastInventory != ActiveInventory;
                OpenInventories = _inventories.Where(o => o.IsVisible);
            }
        }

        public void ClearHighlights() {
            foreach (Inventory inventory in _inventories) {
                inventory.ClearHighlights();
            }
        }

        public void Dispose() {
            _inventories.Clear();
        }
    }
}
