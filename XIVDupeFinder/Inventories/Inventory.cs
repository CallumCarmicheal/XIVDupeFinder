using CriticalCommonLib.Models;

using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Component.GUI;

using XIVDupeFinder;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FFXIVClientStructs.Havok;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;

namespace XIVDupeFinder.Inventories {
    public struct HighlightItem {
        public bool filtered;
        public uint itemId;
        public (byte R, byte G, byte B) colour;
    }

    public abstract unsafe class Inventory {

        public abstract string AddonName { get; }

        protected IntPtr _addon = IntPtr.Zero;
        public IntPtr Addon => _addon;

        public abstract int OffsetX { get; }

        protected AtkUnitBase* _node => (AtkUnitBase*)_addon;

        protected List<List<HighlightItem>>? _filter { get; set; } = null!;
        protected abstract List<List<HighlightItem>> GetEmptyFilter();

        protected UniquePastelColorGenerator uniqueColourGen = new UniquePastelColorGenerator();
        protected Dictionary<uint, (byte R, byte G, byte B)> dictItemColours = new();

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
                .Where(item => item.Item.StackSize > 1 && item.FullStack == false)
                .GroupBy(item => item.ItemId + "-" + (item.IsHQ ? "HQ" : "NQ"))
                .Select(group => new {
                    ItemIdHQ = group.Key,
                    Items = group.ToList(),
                    Count = group.Count()
                });

            //bool highlightEnabled = Plugin.Configuration.OnlyDuringKeyModifier
            //    ? KeyboardHelper.Instance?.IsKeyPressed((int)Keys.Menu)
            //    : true;

            foreach (var itemGroups in groupedItems) {
                try {
                    // Highlight if we have more then 1 item
                    bool highlight = itemGroups.Count > 1;// && highlightEnabled;

                    try {
                        foreach (InventoryItem item in itemGroups.Items) {
                            // Map
                            int bagIndex = ContainerIndex(item) - FirstBagOffset;
                            if (_filter.Count > bagIndex) {
                                List<HighlightItem> bag = _filter[bagIndex];
                                int slot = GridItemCount - 1 - item.SortedSlotIndex;
                                if (bag.Count > slot) {
                                    (byte R, byte G, byte B) rgb;
                                    if (dictItemColours.ContainsKey(item.ItemId) == false) {
                                        rgb = uniqueColourGen.GetColorNormalisedByte(0, 100);
                                        dictItemColours.Add(item.ItemId, rgb);
                                    } else {
                                        rgb = dictItemColours[item.ItemId];
                                    }

                                    bag[slot] = new() { filtered = highlight, itemId = item.ItemId, colour = rgb };
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
                HighlightItem highlightData = new() { filtered = false, itemId = 0 };
                if (_filter != null && _filter[bagIndex].Count > j - startIndex) {
                    highlightData = _filter[bagIndex][j - startIndex];
                }

                SetNodeHighlight(grid->UldManager.NodeList[j], highlightData);
            }
        }

        protected static unsafe void SetNodeHighlight(AtkResNode* node, HighlightItem hd) {
            if (Plugin.Configuration.HighlightRandomColours) {
                byte r = 99, g = 99, b = 99;

                if (hd.filtered)
                    (r, g, b) = hd.colour;

                node->MultiplyRed = r;
                node->MultiplyGreen = g;
                node->MultiplyBlue = b;
            }

            else {
                node->MultiplyRed   = hd.filtered ? Plugin.Configuration.ItemHighlightColour[0] : (byte)100;
                node->MultiplyGreen = hd.filtered ? Plugin.Configuration.ItemHighlightColour[1] : (byte)100;
                node->MultiplyBlue  = hd.filtered ? Plugin.Configuration.ItemHighlightColour[2] : (byte)100;
            }
        }

        public static unsafe void SetTabHighlight(AtkResNode* tab, bool highlight) {
            tab->MultiplyRed = highlight ? Plugin.Configuration.TabHighlightColour[0] : (byte)100;
            tab->MultiplyGreen = highlight ? Plugin.Configuration.TabHighlightColour[1] : (byte)100;
            tab->MultiplyBlue = highlight ? Plugin.Configuration.TabHighlightColour[2] : (byte)100;
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


    public class UniquePastelColorGenerator {
        private HashSet<(int R, int G, int B)> generatedColors = new HashSet<(int, int, int)>();
        private Random random = new Random();

        public (int R, int G, int B) GenerateColor() {
            int maxAttempts = 100;
            (int R, int G, int B) color = (0, 0, 0);
            for (int attempt = 0; attempt < maxAttempts; attempt++) {
                color = GenerateUniquePastelColor();
                if (IsColorDistinct(color)) {
                    generatedColors.Add(color);
                    return color;
                }
            }

            // return the last colour
            return color;
            //throw new Exception("Unable to generate a distinct color.");
        }

        public (byte R, byte G, byte B) GetColorNormalisedByte(int normMin, int normMax) {
            var rgb = GenerateColor();
            (byte R, byte G, byte B) ret = (0, 0, 0);

            ret.R = NormalizeValueToRange(rgb.R, normMin, normMax);
            ret.G = NormalizeValueToRange(rgb.G, normMin, normMax);
            ret.B = NormalizeValueToRange(rgb.B, normMin, normMax);

            return ret;
        }

        private (int R, int G, int B) GenerateUniquePastelColor() {
            int hue = random.Next(0, 360); // Random hue value
            double saturation = 0.5; // Constant value for pastel saturation
            double value = 0.8; // Constant value for pastel brightness

            double angle = hue * Math.PI / 180.0;
            double x = Math.Cos(angle) * saturation;
            double y = Math.Sin(angle) * saturation;
            int r = Convert.ToInt32((x + 1) * value * 255);
            int g = Convert.ToInt32((y + 1) * value * 255);
            int b = Convert.ToInt32((x + y + 2) * value * 255);

            return (r, g, b);
        }

        private bool IsColorDistinct((int R, int G, int B) color) {
            foreach (var existingColor in generatedColors) {
                double distance = CalculateColorDistance(color, existingColor);
                if (distance < 60) // Adjust this threshold to control similarity
                {
                    return false;
                }
            }
            return true;
        }

        private double CalculateColorDistance((int R, int G, int B) color1, (int R, int G, int B) color2) {
            int rDiff = color1.R - color2.R;
            int gDiff = color1.G - color2.G;
            int bDiff = color1.B - color2.B;

            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }


        static byte NormalizeValueToRange(int value, int min, int max) {
            return (byte)(((double)value / 255) * (max - min) + min);
        }
    }
}
