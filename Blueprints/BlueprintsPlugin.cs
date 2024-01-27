using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Blueprints.Patches;
using EquinoxsModUtils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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

        public static LayerMask? buildablesMask = null;
        public static Blueprint clipboard = null;
        public const bool debugMode = false;

        private void Awake() {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            copyShortcut = Config.Bind("General", copyShortcutKey, new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl));
            pasteShortcut = Config.Bind("General", pasteShortcutKey, new KeyboardShortcut(KeyCode.V, KeyCode.LeftControl));
            cwRotateShortcut = Config.Bind("General", cwRotateKey, new KeyboardShortcut(KeyCode.Z));
            ccwRotateShortcut = Config.Bind("General", ccwRotateKey, new KeyboardShortcut(KeyCode.Z, KeyCode.LeftControl));
            blueprintsShortcut = Config.Bind("General", blueprintsKey, new KeyboardShortcut(KeyCode.C));

            Harmony.CreateAndPatchAll(typeof(PlayerInspectorPatch));

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }

        private void Update() {
            if (copyShortcut.Value.IsDown()) {
                if (!MachineCopier.isCopying) {
                    MachineCopier.startCopying();
                }
                else {
                    MachineCopier.endCopying();
                }
            }

            if (pasteShortcut.Value.IsDown()) {
                if (!MachinePaster.isPasting) {
                    MachinePaster.startPasting();
                }
                else {
                    MachinePaster.endPasting();
                }
            }

            if (blueprintsShortcut.Value.IsDown() && !UI.isOpen) {
                UI.show();
            }

            if (MachineCopier.isCopying) MachineCopier.updateEndPosition();
            if (MachinePaster.isPasting) MachinePaster.updateDisplayBox();
        }

        // Public Functions

        public static Vector3? getLookedAtMachinePos() {
            Camera cam = Player.instance.cam;
            GenericMachineInstanceRef machineRef = GenericMachineInstanceRef.INVALID_REFERENCE;
            RaycastHit raycastHit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out raycastHit, 1000, (LayerMask)buildablesMask)) {
                machineRef = FHG_Utils.FindMachineRef(raycastHit.collider.gameObject);
                if (machineRef.instanceId != GenericMachineInstanceRef.INVALID_REFERENCE.instanceId && machineRef.MyGridInfo.Center != null) {
                    return machineRef.MyGridInfo.Center;
                }
            }

            return null;
        }

        public static Vector3? getAboveLookedAtMachinePos() {
            Camera cam = Player.instance.cam;
            GenericMachineInstanceRef machineRef = GenericMachineInstanceRef.INVALID_REFERENCE;
            RaycastHit raycastHit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out raycastHit, 1000, (LayerMask)buildablesMask)) {
                machineRef = FHG_Utils.FindMachineRef(raycastHit.collider.gameObject);
                if(machineRef.instanceId != GenericMachineInstanceRef.INVALID_REFERENCE.instanceId && machineRef.MyGridInfo.Center != null) {
                    Vector3 result = machineRef.MyGridInfo.Center;
                    result.y += machineRef.MyGridInfo.dims.y;
                    return result;
                }
            }

            return null;
        }

        public static Vector3Int getRoundedAimLocation() {
            Vector3 placement = Player.instance.builder.CurrentAimTarget;
            return new Vector3Int() {
                x = (int)(placement.x >= 0 ? Math.Floor(placement.x) : Math.Ceiling(placement.x)),
                y = (int)(placement.y >= 0 ? Math.Floor(placement.y) : Math.Ceiling(placement.y)),
                z = (int)(placement.z >= 0 ? Math.Floor(placement.z) : Math.Ceiling(placement.z)),
            };
        }

        public static Vector3Int getMinPosOfAimedMachine() {
            FieldInfo targetMachineInfo = typeof(PlayerInteraction).GetField("targetMachineRef", BindingFlags.Instance | BindingFlags.NonPublic);
            GenericMachineInstanceRef machine = (GenericMachineInstanceRef)targetMachineInfo.GetValue(Player.instance.interaction);
            return machine.GetGridInfo().minPos;
        }

        public static Vector3Int getAboveMinPosOfAimedMachine() {
            Vector3Int pos = getMinPosOfAimedMachine();
            ++pos.y;
            return pos;
        }
    }
}
