using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using XIVDupeFinder.Windows;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Ui;
using CriticalCommonLib;
using System.Reflection;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Newtonsoft.Json.Linq;
using Dalamud.Game;
using System;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using XIVDupeFinder.Inventories;
using CriticalCommonLib.Crafting;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Time;
using Dalamud.Data;
using Dalamud.Logging;
using Dalamud.Game.ClientState;
using ImGuiNET;
using Dalamud.Game.ClientState.Keys;

namespace XIVDupeFinder {
    public sealed class Plugin : IDalamudPlugin, IDisposable {
        public string Name => "XIVDupeFinder";
        private const string CommandName = "/xivdupes";

        private static Plugin? _instance = null!;
        public static Plugin? Instance { get => _instance; }

    // Dalamud Properties
        [PluginService, RequiredVersion("1.0")] public static ClientState ClientState { get; private set; } = null!;
        [PluginService, RequiredVersion("1.0")] private static DalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService, RequiredVersion("1.0")] private static CommandManager CommandManager { get; set; } = null!;
        [PluginService, RequiredVersion("1.0")] public static Framework Framework { get; private set; } = null!;
        [PluginService, RequiredVersion("1.0")] public static GameGui GameGui { get; private set; } = null!;
        [PluginService, RequiredVersion("1.0")] public static DataManager Data { get; private set; } = null!;

        [PluginService, RequiredVersion("1.0")] public static KeyState KeyState { get; private set; } = null!;
        
        // Own Windows, Properties
        public static Configuration Configuration { get; private set; } = null!;
        public static WindowSystem WindowSystem = new("XIVDupeFinder");
        private static InventoriesManager _manager = null!;

        private ConfigWindow ConfigWindow { get; init; }

    // Critical Common Libs 
        public static FrameworkService FrameworkService { get; private set; } = null!;
        public static GameInterface GameInterface { get; private set; } = null!;
        public static CharacterMonitor CharacterMonitor { get; private set; } = null!;
        public static GameUiManager GameUi { get; private set; } = null!;
        public static OdrScanner OdrScanner { get; private set; } = null!;
        public static InventoryMonitor InventoryMonitor { get; private set; } = null!;
        public static InventoryScanner InventoryScanner { get; private set; } = null!;
        public static CraftMonitor CraftMonitor { get; private set; } = null!;

        public Plugin() {
            _instance = this;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            //KeyboardHelper.Initialize();

            Service? service = PluginInterface.Create<Service>(); 
            Service.SeTime = new SeTime();
            Service.ExcelCache = new ExcelCache(Data, false, false, false);
            Service.ExcelCache.PreCacheItemData();

            FrameworkService = new FrameworkService(Framework);
            GameInterface = new GameInterface();
            CharacterMonitor = new CharacterMonitor();
            GameUi = new GameUiManager();
            CraftMonitor = new CraftMonitor(GameUi);
            OdrScanner = new OdrScanner(CharacterMonitor);
            InventoryScanner = new InventoryScanner(CharacterMonitor, GameUi, GameInterface, OdrScanner);
            InventoryMonitor = new InventoryMonitor(CharacterMonitor, CraftMonitor, InventoryScanner, FrameworkService);
            InventoryScanner.Enable();

            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
                HelpMessage = "Displays the configuration window for inventory dupe finder."
            });

            Framework.Update += Update;
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            _manager = new InventoriesManager();

            // Hook game events
        }

        private void Update(Framework framework) {
            _manager.Update();
        }

        public static unsafe void ClearHighlights() {
            _manager?.ClearHighlights();
        }

        public void Dispose() {
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();

            CommandManager.RemoveHandler(CommandName);

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing) {
            if (!disposing) 
                return;

            // Remove tick and framework events
            Framework.Update -= Update;
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

            // Reset our highlights
            try { ClearHighlights(); } catch { }

            // Remove our crit lib components
            InventoryMonitor.Dispose();
            InventoryScanner.Dispose();

            FrameworkService.Dispose();
            GameUi.Dispose();
            CharacterMonitor.Dispose();
            CraftMonitor.Dispose();
            OdrScanner.Dispose();
            GameInterface.Dispose();

            Service.ExcelCache.Destroy();

            // Remove our manager
            _manager.Dispose();
        }

        private void OnCommand(string command, string args) {
            // In response to the slash command, just display our main ui
            ConfigWindow.IsOpen = true;
        }

        private unsafe void DrawUI() {
            WindowSystem.Draw();
            DrawInventoryHighlights();
        }

        private unsafe void DrawInventoryHighlights() {
            // Check if any required variables are null
            if (Configuration == null || ClientState.LocalPlayer == null || _manager == null)
                return;

            // Check if we are enabled or disabled.
            var enabled = Plugin.Configuration.HighlightDuplicates;
            if (enabled == false)
                return;

            // If we do not have an active inventory then reset the highlights.
            if (_manager.ActiveInventory == null) {
                _manager.ClearHighlights();
                return;
            }

            // Apply our highlighting
            bool highlightEnabled = Plugin.Configuration.OnlyDuringKeyModifier
                   ? KeyState[VirtualKey.MENU]
                   : enabled;

            // If we are highlighting the duplicates
            if (highlightEnabled) {
                // If we are highlighting multiple windows
                if (Configuration.HighlightOnlyActiveWindow == false) {
                    foreach (var inventory in _manager.OpenInventories) {
                        if (inventory == null) continue;

                        inventory.DiscoverDuplicates();
                        inventory.UpdateHighlights();
                    }
                }

                // Only highlight active window
                else {
                    // Check if we have changed inventory and remove colours from the last
                    if (_manager.ChangedActiveInventory && _manager.LastInventory != null)
                        _manager.LastInventory.ClearHighlights();

                    _manager.ActiveInventory.DiscoverDuplicates();
                    _manager.ActiveInventory.UpdateHighlights();
                }
            }
            // If we are not highlighting anything, clear the highlights.
            else {
                _manager.ClearHighlights();
            }
        }

        public void DrawConfigUI() {
            ConfigWindow.IsOpen = true;
        }
    }
}
