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

namespace XIVDupeFinder {
    public sealed class Plugin : IDalamudPlugin, IDisposable {
        public string Name => "XIVDXIVDupeFinderupeFinder";
        private const string CommandName = "/xlinvdupes";

        // Dalamud Properties
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] private static DalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] private static CommandManager CommandManager { get; set; } = null!;
        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static GameGui GameGui { get; private set; } = null!;
        [PluginService] public static DataManager Data { get; private set; } = null!;
        
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
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            Service? service = PluginInterface.Create<Service>(); 
            Service.SeTime = new SeTime();
            Service.ExcelCache = new ExcelCache(Data, false, false, false);
            Service.ExcelCache.PreCacheItemData();

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

            ClearHighlights();

            InventoryMonitor.Dispose();
            GameUi.Dispose();
            CharacterMonitor.Dispose();
            CraftMonitor.Dispose();
            InventoryScanner.Dispose();
            OdrScanner.Dispose();

            GameInterface.Dispose();
            Service.ExcelCache.Destroy();

            _manager.Dispose();

            Framework.Update += Update;
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        private void OnCommand(string command, string args) {
            // in response to the slash command, just display our main ui
            ConfigWindow.IsOpen = true;
        }
        
        private unsafe void DrawUI() {
            WindowSystem.Draw();

            if (Configuration == null || ClientState.LocalPlayer == null || _manager == null) 
                return;

            // If we do not have an active inventory close.
            if (_manager.ActiveInventory == null)
                return;

            // Apply our highlighting
            if (Configuration.HighlightDuplicates) {
                _manager.ActiveInventory.DiscoverDuplicates();
                _manager.ActiveInventory.UpdateHighlights();
            }
        }

        public void DrawConfigUI() {
            ConfigWindow.IsOpen = true;
        }
    }
}
