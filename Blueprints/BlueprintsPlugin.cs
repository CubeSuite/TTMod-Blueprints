using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Blueprints.Patches;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Blueprints
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class BlueprintsPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.Blueprints";
        private const string PluginName = "Blueprints";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        public static string copyShortcutKey = "Copy Shortcut";
        public static string pasteShortcutKey = "Paste Shortcut";
        public static string cwRotateKey = "CW Rotation Shortcut";
        public static string ccwRotateKey = "CCW Rotation Shortcut";
        public static string blueprintsKey = "Open Blueprints Shortcut";

        public static ConfigEntry<KeyboardShortcut> copyShortcut;
        public static ConfigEntry<KeyboardShortcut> pasteShortcut;
        public static ConfigEntry<KeyboardShortcut> cwRotateShortcut;
        public static ConfigEntry<KeyboardShortcut> ccwRotateShortcut;
        public static ConfigEntry<KeyboardShortcut> blueprintsShortcut;

        public static Blueprint clipboard = null;
        public const bool debugMode = false;

        private void Awake() {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            copyShortcut = Config.Bind("General", copyShortcutKey, new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl));
            pasteShortcut = Config.Bind("General", pasteShortcutKey, new KeyboardShortcut(KeyCode.V, KeyCode.LeftControl));
            cwRotateShortcut = Config.Bind("General", cwRotateKey, new KeyboardShortcut(KeyCode.Z));
            ccwRotateShortcut = Config.Bind("General", ccwRotateKey, new KeyboardShortcut(KeyCode.Z, KeyCode.LeftControl));
            blueprintsShortcut = Config.Bind("General", blueprintsKey, new KeyboardShortcut(KeyCode.X, KeyCode.LeftControl));

            Harmony.CreateAndPatchAll(typeof(PlayerInspectorPatch));
            Harmony.CreateAndPatchAll(typeof(PauseMenuPatch));

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;

            UI.start();
        }

        private void Update() {
            if (copyShortcut.Value.IsDown() && !UI.isOpen) {
                if (!MachineCopier.isCopying) {
                    MachineCopier.startCopying();
                }
                else {
                    MachineCopier.endCopying();
                }
            }

            if (pasteShortcut.Value.IsDown() && !UI.isOpen && !MachineCopier.isCopying) {
                if (!MachinePaster.isPasting) {
                    MachinePaster.startPasting();
                }
                else {
                    MachinePaster.endPasting();
                }
            }

            if (blueprintsShortcut.Value.IsDown()) {
                if (!UI.isOpen) UI.show();
                else UI.hide();
            }

            if (MachineCopier.isCopying) MachineCopier.updateEndPosition();
            if (MachinePaster.isPasting) MachinePaster.updateDisplayBox();
        }

        // Public Functions

        public static void Notify(string message) {
            Debug.Log(message);
            UIManager.instance.systemLog.FlashMessage(new SystemMessageInfo(message));
        }

        public static void saveClipboardToFile() {
            File.WriteAllText(UI.curerntBlueprintFile, clipboard.toJson());
        }

        public static void loadFileToClipboard() {
            if (!File.Exists(UI.curerntBlueprintFile)) return;
            string json = File.ReadAllText(UI.curerntBlueprintFile);
            clipboard = JsonConvert.DeserializeObject<Blueprint>(json);
            clipboard.setSize(clipboard.size.asUnityVector3());

            if(clipboard.chestSizes.Count == 0) {
                for(int i = 0; i < clipboard.machineIDs.Count; i++) {
                    clipboard.chestSizes.Add(0);
                }
            }
        }
    }
}
