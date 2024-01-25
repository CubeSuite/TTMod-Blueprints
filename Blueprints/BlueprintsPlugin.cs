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

        public static ConfigEntry<KeyboardShortcut> copyShortcut;
        public static ConfigEntry<KeyboardShortcut> pasteShortcut;

        public static LayerMask? buildablesMask = null;
        public static Blueprint clipboard = null;

        private void Awake() {
            
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            copyShortcut = Config.Bind("General", copyShortcutKey, new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl));
            pasteShortcut = Config.Bind("General", pasteShortcutKey, new KeyboardShortcut(KeyCode.V, KeyCode.LeftControl));

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
                pasteClipboard();
            }

            if (MachineCopier.isCopying) {
                MachineCopier.updateEndPosition();
            }
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

        // Private Functions

        private void pasteClipboard() {
            if(clipboard == null) {
                Debug.Log("Can't paste null clipboard");
                return;
            }

            foreach(MachineCost cost in clipboard.getCost()) {
                ResourceInfo info = SaveState.GetResInfoFromId(cost.resId);
                if(!Player.instance.inventory.myInv.HasResources(info, cost.count)) {
                    Debug.Log($"Not enough {info.displayName} {Player.instance.inventory.myInv.GetResourceCount(cost.resId)}/{cost.count}");
                    return;
                }
            }

            foreach (MachineCost cost in clipboard.getCost()) {
                Player.instance.inventory.myInv.TryRemoveResources(cost.resId, cost.count);
            }

            Vector3? placementResult = getAboveLookedAtMachinePos();
            if(placementResult == null) {
                Debug.Log("Can't paste on non-machine");
                return;
            }

            Vector3 placement = (Vector3)placementResult;
            Debug.Log($"Pasting clipboard at {placement}");

            for(int i = 0; i < clipboard.machines.Count; i++) {
                IMachineInstanceRef machine = clipboard.machines[i];

                Vector3Int newMinPos = new Vector3Int();
                if(placement.x >= 0) {
                    newMinPos.x = (int)Math.Ceiling(placement.x + clipboard.machineRelativePositions[i].x);
                }
                else {
                    newMinPos.x = (int)Math.Floor(placement.x + clipboard.machineRelativePositions[i].x);
                }
                if (placement.y >= 0) {
                    newMinPos.y = (int)Math.Ceiling(placement.y + clipboard.machineRelativePositions[i].y);
                }
                else {
                    newMinPos.y = (int)Math.Floor(placement.y + clipboard.machineRelativePositions[i].y);
                }
                if (placement.z >= 0) {
                    newMinPos.z = (int)Math.Ceiling(placement.z + clipboard.machineRelativePositions[i].z);
                }
                else {
                    newMinPos.z = (int)Math.Floor(placement.z + clipboard.machineRelativePositions[i].z);
                }

                GridInfo newGridInfo = new GridInfo();
                newGridInfo.CopyFrom(machine.gridInfo);

                if (machine.typeIndex != MachineTypeEnum.Conveyor) {
                    SimpleBuildInfo info = new SimpleBuildInfo() {
                        machineType = machine.GetCommonInfo().resId,
                        rotation = machine.GetGridInfo().YawRotation,
                        minGridPos = newMinPos,

                    };
                    info.tick += 5;
                    SimpleBuildAction action = new SimpleBuildAction() {
                        info = (SimpleBuildInfo)info.Clone(),
                        resourceCostAmount = 1,
                        resourceCostID = (int)machine.typeIndex,
                    };

                    newGridInfo.minPos = newMinPos;
                    if (!GridManager.instance.CheckBuildableIncludingVoxelsAt(newGridInfo)) {
                        Debug.Log($"Can't build at {placement} | {newMinPos}");
                        return;
                    }
                    else {
                        Debug.Log($"Building at {newMinPos}");
                    }

                    NetworkMessageRelay.instance.SendNetworkAction(action);
                    Debug.Log($"Sent network message relay");
                }
                else {

                }
            }
        }
    }
}
