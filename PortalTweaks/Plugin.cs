using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using PortalTweaks.Behaviors;
using ServerSync;

namespace PortalTweaks
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class PortalTweaksPlugin : BaseUnityPlugin
    {
        internal const string ModName = "PortalTweaks";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private static readonly string ConfigFileName = ModGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource PortalTweaksLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public static PortalTweaksPlugin _Plugin = null!;
        public enum Toggle { On = 1, Off = 0 }

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        public static ConfigEntry<string> _chargeItem = null!;
        public static ConfigEntry<int> _chargeMax = null!;
        public static ConfigEntry<int> _chargeDecay = null!;
        public static ConfigEntry<int> _cost = null!;
        public static ConfigEntry<Toggle> _TeleportAnything = null!;
        public static ConfigEntry<Toggle> _UseKeys = null!;
        public static ConfigEntry<Toggle> _TeleportTames = null!;
        public static ConfigEntry<string> _Required = null!;
        public static ConfigEntry<string> _toCharge = null!;
        public static ConfigEntry<string> _fullyCharged = null!;
        public static ConfigEntry<string> _addCharge = null!;

        private void InitConfigs()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            _chargeItem = config("2 - Settings", "Charge Item", "GreydwarfEye", "Set charge item");
            _chargeMax = config("2 - Settings", "Charge Max", 10, "Set max charge");
            _chargeDecay = config("2 - Settings", "Charge Decay", 5, "Set loss of charge time in minutes");
            _cost = config("2 - Settings", "Cost", 1, "Set charge cost to teleport");
            _TeleportAnything = config("2 - Settings", "Teleport Anything", Toggle.Off, "If on, player can teleport non-teleportable items");
            _UseKeys = config("2 - Settings", "Use Keys", Toggle.Off, "If on, portal checks if game has global key to allow teleportation of non-teleportable items");
            _TeleportTames = config("2 - Settings", "Teleport Tames", Toggle.Off, "If on, portal can teleport tames that are following player");
            
            _Required = config("Localization", "Required", "Required", "");
            _toCharge = config("Localization", "to charge", "to charge", "");
            _fullyCharged = config("Localization", "Portal is fully charged", "Portal is fully charged", "");
            _addCharge = config("Localization", "Add charge", "Add charge", "");
        }
        public void Awake()
        {
            _Plugin = this;
            
            InitConfigs();


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                PortalTweaksLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                PortalTweaksLogger.LogError($"There was an issue loading your {ConfigFileName}");
                PortalTweaksLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
        
        public ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        public ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }
    }
}