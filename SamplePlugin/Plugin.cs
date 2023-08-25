using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using InvDupeFinder.Windows;
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
using InventorySearchBar.Inventories;
using CriticalCommonLib.Crafting;

namespace InvDupeFinder {
    public sealed class Plugin : IDalamudPlugin, IDisposable {
        public string Name => "Inventory Dupe Finder";
        private const string CommandName = "/xlinvdupes";

    // Dalamud Properties
        private static DalamudPluginInterface PluginInterface { get; set; } = null!;
        private static CommandManager CommandManager { get; set; } = null!;
        public static Framework Framework { get; private set; } = null!;
        public static GameGui GameGui { get; private set; } = null!;

    // Own Windows, Properties
        public static Configuration Configuration { get; private set; } = null!;
        public static WindowSystem WindowSystem = new("InvDupeFinder");
        private static InventoriesManager _manager = null!;

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

    // Critical Common Libs 
        public static FrameworkService FrameworkService { get; private set; } = null!;
        public static GameInterface GameInterface { get; private set; } = null!;
        public static CharacterMonitor CharacterMonitor { get; private set; } = null!;
        public static GameUiManager GameUi { get; private set; } = null!;
        public static OdrScanner OdrScanner { get; private set; } = null!;
        public static InventoryMonitor InventoryMonitor { get; private set; } = null!;
        public static InventoryScanner InventoryScanner { get; private set; } = null!;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager         commandManager,
            [RequiredVersion("1.0")] GameGui                gameGui,
            [RequiredVersion("1.0")] Framework              framwork
        ) {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            Framework = framwork;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            InvDupeFinder.Configuration.Initialize(PluginInterface);

            pluginInterface.Create<Service>();
            Service.ExcelCache = new ExcelCache(Service.Data);
            FrameworkService = new FrameworkService(Service.Framework);
            GameInterface = new GameInterface();
            CharacterMonitor = new CharacterMonitor();
            GameUi = new GameUiManager();
            OdrScanner = new OdrScanner(CharacterMonitor);
            InventoryScanner = new InventoryScanner(CharacterMonitor, GameUi, GameInterface, OdrScanner);

            // you might normally want to embed resources and load them from the manifest stream
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            var goatImage = PluginInterface.UiBuilder.LoadImage(imagePath);

            this.ConfigWindow = new ConfigWindow(this);
            this.MainWindow = new MainWindow(this, goatImage);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

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
            MainWindow.Dispose();

            CommandManager.RemoveHandler(CommandName);

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing) {
            if (!disposing) {
                return;
            }

            ClearHighlights();

            //InventoryMonitor.Dispose();
            GameUi.Dispose();
            CharacterMonitor.Dispose();
            //CraftMonitor.Dispose();
            InventoryScanner.Dispose();
            OdrScanner.Dispose();

            GameInterface.Dispose();
            Service.ExcelCache.Destroy();

            //_windowSystem.RemoveAllWindows();
            _manager.Dispose();

            Framework.Update += Update;
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        private void OnCommand(string command, string args) {
            // in response to the slash command, just display our main ui
            MainWindow.IsOpen = true;
        }

        private void DrawUI() {
            WindowSystem.Draw();
        }

        public void DrawConfigUI() {
            ConfigWindow.IsOpen = true;
        }
    }
}
