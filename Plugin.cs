using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace LGUGui
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("com.malco.lethalcompany.moreshipupgrades")]
    public class Plugin : BaseUnityPlugin
    {
        
        private const string modGUID = "Slayer6409.LGU_GUI";
        private const string modName = "LGU_GUI";
        private const string modVersion = "0.0.1";

        public static AssetBundle LoadedAssets;
        public static ManualLogSource CustomLogger;
        public static GameObject NetworkerPrefab, PurchaseMenuPrefab, UpgradeButton, TradePrefab;
        public static ConfigFile BepInExConfig = null;
        internal static IngameKeybinds Keybinds = null!;
        
        private readonly Harmony harmony = new Harmony(modGUID);
        
        public static ConfigEntry<bool> onlyInOrbit;
        public static ConfigEntry<bool> onlyOnShip;
        public static ConfigEntry<bool> extendedLog;
        private void Awake()
        { 

            
            CustomLogger = BepInEx.Logging.Logger.CreateLogSource("LateGameUpgrades GUI");
            BepInExConfig = new ConfigFile(Path.Combine(Paths.ConfigPath, "LGU_GUI.cfg"),true);
            LoadedAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lgugui"));

            PurchaseMenuPrefab  = LoadedAssets.LoadAsset<GameObject>("PurchaseGUI");
            UpgradeButton = LoadedAssets.LoadAsset<GameObject>("UpgradePrefab");
            TradePrefab = LoadedAssets.LoadAsset<GameObject>("TradePrefab");
            configSetup();

            harmony.PatchAll();
            Keybinds  = new IngameKeybinds();
            Keybinds.PurchaseMenu.performed += context => tryShowMenu();

            CustomLogger.LogInfo($"Plugin LGUGui is loaded!");
        }

        public static void ExtendedLogging(string msg, LogLevel level = LogLevel.Info)
        {
            if(extendedLog.Value) CustomLogger.Log(level, msg);
        }

        public void configSetup()
        {
            onlyInOrbit = BepInExConfig.Bind<bool>(
                "Main",
                "Only In Orbit",
                false,
                "Makes it to where you are only able to upgrade in orbit.");
            onlyOnShip = BepInExConfig.Bind<bool>(
                "Main",
                "Only On Ship",
                true,
                "Makes it to where you are only able to upgrade pn the ship.");
            extendedLog = BepInExConfig.Bind<bool>(
                "Main",
                "Extended Logging",
                false,
                "Log more debug stuff");
        }

        public void tryShowMenu()
        {
            if(StartOfRound.Instance == null) return;
            if(StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen) return;
            if (onlyInOrbit.Value)
                if (!StartOfRound.Instance.inShipPhase) return;
            if(onlyOnShip.Value)
                if (!StartOfRound.Instance.localPlayerController.isInHangarShipRoom)return;
            if(StartOfRound.Instance.localPlayerController.inTerminalMenu) return;
            if(StartOfRound.Instance.localPlayerController.isTypingChat) return;
            PurchaseMenu.initMenu();
            
        }
    }
}

