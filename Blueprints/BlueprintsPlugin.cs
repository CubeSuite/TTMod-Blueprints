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
        private const string VersionString = "1.1.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        #region Keybinds

        private static string copyShortcutKey = "Copy Shortcut";
        private static string pasteShortcutKey = "Paste Shortcut";
        private static string cancelShortcutKey = "Cancel Shortcut";
        private static string cwRotateKey = "CW Rotation Shortcut";
        private static string ccwRotateKey = "CCW Rotation Shortcut";
        private static string blueprintsKey = "Open Blueprints Shortcut";

        private static string nudgeLeftKey = "Nudge Left";
        private static string nudgeRightKey = "Nudge Right";
        private static string nudgeForwardKey = "Nudge Forward";
        private static string nudgeBackwardKey = "Nudge Backward";
        private static string nudgeUpKey = "Nudge Up";
        private static string nudgeDownKey = "Nudge Down";

        private static string shrinkLeftKey = "Shrink Copy Region Left";
        private static string shrinkRightKey = "Shrink Copy Region Right";
        private static string shrinkForwardKey = "Shrink Copy Region Forward";
        private static string shrinkBackwardKey = "Shrink Copy Region Backward";
        private static string shrinkUpKey = "Shrink Copy Region Up";
        private static string shrinkDownKey = "Shrink Copy Region Down";

        public static ConfigEntry<KeyboardShortcut> copyShortcut;
        public static ConfigEntry<KeyboardShortcut> pasteShortcut;
        public static ConfigEntry<KeyboardShortcut> cancelShortcut;
        public static ConfigEntry<KeyboardShortcut> cwRotateShortcut;
        public static ConfigEntry<KeyboardShortcut> ccwRotateShortcut;
        public static ConfigEntry<KeyboardShortcut> blueprintsShortcut;

        public static ConfigEntry<KeyboardShortcut> nudgeLeftShortcut;
        public static ConfigEntry<KeyboardShortcut> nudgeRightShortcut;
        public static ConfigEntry<KeyboardShortcut> nudgeForwardShortcut;
        public static ConfigEntry<KeyboardShortcut> nudgeBackwardShortcut;
        public static ConfigEntry<KeyboardShortcut> nudgeUpShortcut;
        public static ConfigEntry<KeyboardShortcut> nudgeDownShortcut;

        public static ConfigEntry<KeyboardShortcut> shrinkLeftShortcut;
        public static ConfigEntry<KeyboardShortcut> shrinkRightShortcut;
        public static ConfigEntry<KeyboardShortcut> shrinkForwardShortcut;
        public static ConfigEntry<KeyboardShortcut> shrinkBackwardShortcut;
        public static ConfigEntry<KeyboardShortcut> shrinkUpShortcut;
        public static ConfigEntry<KeyboardShortcut> shrinkDownShortcut;

        #endregion

        // Objects & Variables
        public static Blueprint clipboard = null;
        public static List<IMachineInstanceRef> machinesToCopy = new List<IMachineInstanceRef>();
        public static List<NetworkAction> machinesToBuild = new List<NetworkAction>();

        // Unity Functions

        private void Awake() {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            bindConfigEntries();
            applyPatches();

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;

            UI.start();
        }

        private void Update() {
            if (copyShortcut.Value.IsDown() && !UI.isOpen && !MachinePaster.isPasting) {
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

            if (blueprintsShortcut.Value.IsDown() && !UI.isOpen) {
                UI.show();
            }

            if (File.Exists(UI.resumeFile)) {
                File.Delete(UI.resumeFile);
                UIManager.instance.pauseMenu.Close();
            }

            if (MachineCopier.isCopying) MachineCopier.updateEndPosition();
            if (MachinePaster.isPasting) MachinePaster.updateHolograms();
        }

        private void FixedUpdate() {
            if(machinesToCopy.Count != 0) {
                IMachineInstanceRef machine = machinesToCopy[0];
                addMachineToBlueprint(machine);
                machinesToCopy.RemoveAt(0);
                if(machinesToCopy.Count == 0) {
                    saveClipboardToFile();
                    Notify("Region copied!");
                }
            }

            if(machinesToBuild.Count != 0) {
                NetworkMessageRelay.instance.SendNetworkAction(machinesToBuild[0]);
                machinesToBuild.RemoveAt(0);
                if(machinesToBuild.Count == 0) {
                    Notify("Finished pasting");
                    MachinePaster.hideHolograms();
                }
            }
        }

        // Public Functions

        public static void Notify(string message) {
            Debug.Log(message);
            UIManager.instance.systemLog.FlashMessage(message);
        }

        public static void saveClipboardToFile() {
            File.WriteAllText(UI.currentBlueprintFile, clipboard.toJson());
        }

        public static void loadFileToClipboard() {
            if (!File.Exists(UI.currentBlueprintFile)) return;
            string json = File.ReadAllText(UI.currentBlueprintFile);
            clipboard = JsonConvert.DeserializeObject<Blueprint>(json);
            clipboard.setSize(clipboard.size.asUnityVector3());

            if(clipboard.chestSizes.Count == 0) {
                for(int i = 0; i < clipboard.machineIDs.Count; i++) {
                    clipboard.chestSizes.Add(0);
                }
            }
        }

        // Private Functions

        private void bindConfigEntries() {
            copyShortcut = Config.Bind("General", copyShortcutKey, new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl));
            pasteShortcut = Config.Bind("General", pasteShortcutKey, new KeyboardShortcut(KeyCode.V, KeyCode.LeftControl));
            cancelShortcut = Config.Bind("General", cancelShortcutKey, new KeyboardShortcut(KeyCode.Backspace));
            cwRotateShortcut = Config.Bind("General", cwRotateKey, new KeyboardShortcut(KeyCode.Z));
            ccwRotateShortcut = Config.Bind("General", ccwRotateKey, new KeyboardShortcut(KeyCode.Z, KeyCode.LeftControl));
            blueprintsShortcut = Config.Bind("General", blueprintsKey, new KeyboardShortcut(KeyCode.X, KeyCode.LeftControl));

            nudgeLeftShortcut = Config.Bind("Nudge", nudgeLeftKey, new KeyboardShortcut(KeyCode.LeftArrow));
            nudgeRightShortcut = Config.Bind("Nudge", nudgeRightKey, new KeyboardShortcut(KeyCode.RightArrow));
            nudgeForwardShortcut = Config.Bind("Nudge", nudgeForwardKey, new KeyboardShortcut(KeyCode.UpArrow));
            nudgeBackwardShortcut = Config.Bind("Nudge", nudgeBackwardKey, new KeyboardShortcut(KeyCode.DownArrow));
            nudgeUpShortcut = Config.Bind("Nudge", nudgeUpKey, new KeyboardShortcut(KeyCode.UpArrow, KeyCode.LeftShift));
            nudgeDownShortcut = Config.Bind("Nudge", nudgeDownKey, new KeyboardShortcut(KeyCode.DownArrow, KeyCode.LeftShift));

            shrinkLeftShortcut = Config.Bind("Shrink Copy Region", shrinkLeftKey, new KeyboardShortcut(KeyCode.LeftArrow, KeyCode.LeftControl));
            shrinkRightShortcut = Config.Bind("Shrink Copy Region", shrinkRightKey, new KeyboardShortcut(KeyCode.RightArrow, KeyCode.LeftControl));
            shrinkForwardShortcut = Config.Bind("Shrink Copy Region", shrinkForwardKey, new KeyboardShortcut(KeyCode.UpArrow, KeyCode.LeftControl));
            shrinkBackwardShortcut = Config.Bind("Shrink Copy Region", shrinkBackwardKey, new KeyboardShortcut(KeyCode.DownArrow, KeyCode.LeftControl));
            shrinkUpShortcut = Config.Bind("Shrink Copy Region", shrinkUpKey, new KeyboardShortcut(KeyCode.UpArrow, KeyCode.LeftShift, KeyCode.LeftControl));
            shrinkDownShortcut = Config.Bind("Shrink Copy Region", shrinkDownKey, new KeyboardShortcut(KeyCode.DownArrow, KeyCode.LeftShift, KeyCode.LeftControl));
        }

        private void applyPatches() {
            Harmony.CreateAndPatchAll(typeof(PlayerInspectorPatch));
            Harmony.CreateAndPatchAll(typeof(PauseMenuPatch));
        }

        private void addMachineToBlueprint(IMachineInstanceRef machine) {
            bool debugFunction = true;

            clipboard.machineIDs.Add(machine.instanceId);
            clipboard.machineIndexes.Add(machine.index);
            clipboard.machineResIDs.Add(machine.GetCommonInfo().resId);
            clipboard.machineTypes.Add((int)machine.typeIndex);
            clipboard.machineRotations.Add(machine.GetGridInfo().yawRot);
            clipboard.machineDimensions.Add(getDimensions(machine));

            if (debugFunction) {
                Debug.Log($"addMachineToBlueprint() machine.instanceId: {machine.instanceId}");
                Debug.Log($"addMachineToBlueprint() machine.instanceId: {machine.index}");
                Debug.Log($"addMachineToBlueprint() machine.instanceId: {machine.GetCommonInfo().resId}");
                Debug.Log($"addMachineToBlueprint() machine.instanceId: {machine.typeIndex}");
                Debug.Log($"addMachineToBlueprint() machine.instanceId: {machine.GetGridInfo().yawRot}");
                Debug.Log($"addMachineToBlueprint() machine.instanceId: {machine.gridInfo.dims}");
            }

            GenericMachineInstanceRef generic = machine.AsGeneric();

            switch (machine.typeIndex) {
                case MachineTypeEnum.Assembler:
                    AssemblerInstance assembler = generic.Get<AssemblerInstance>();
                    clipboard.machineRecipes.Add(assembler.targetRecipe == null ? -1 : assembler.targetRecipe.uniqueId);
                    clipboard.conveyorShapes.Add(0);
                    clipboard.conveyorBuildBackwards.Add(false);
                    clipboard.chestSizes.Add(0);
                    break;

                case MachineTypeEnum.Inserter:
                    InserterInstance inserter = generic.Get<InserterInstance>();
                    clipboard.machineRecipes.Add(inserter.filterType);
                    clipboard.conveyorShapes.Add(0);
                    clipboard.conveyorBuildBackwards.Add(false);
                    clipboard.chestSizes.Add(0);
                    break;

                case MachineTypeEnum.Conveyor:
                    ConveyorInstance conveyor = generic.Get<ConveyorInstance>();
                    clipboard.machineRecipes.Add(0);
                    clipboard.conveyorShapes.Add((int)conveyor.beltShape);
                    clipboard.conveyorBuildBackwards.Add(conveyor.buildBackwards);
                    clipboard.chestSizes.Add(0);
                    break;

                case MachineTypeEnum.Chest:
                    ChestInstance chest = generic.Get<ChestInstance>();
                    clipboard.machineRecipes.Add(0);
                    clipboard.conveyorShapes.Add(0);
                    clipboard.conveyorBuildBackwards.Add(false);
                    clipboard.chestSizes.Add(chest.commonInfo.inventories[0].numSlots);
                    break;

                default:
                    clipboard.machineRecipes.Add(0);
                    clipboard.conveyorShapes.Add(0);
                    clipboard.conveyorBuildBackwards.Add(false);
                    clipboard.chestSizes.Add(0);
                    break;
            }
        }

        private MyVector3 getDimensions(IMachineInstanceRef machine) {
            switch (machine.gridInfo.yawRot) {
                default:
                case 0:
                case 180: return new MyVector3(machine.gridInfo.dims);

                case 90:
                case 270:
                    return new MyVector3(new Vector3() {
                        x = machine.gridInfo.dims.z,
                        y = machine.gridInfo.dims.y,
                        z = machine.gridInfo.dims.x,
                    });
            }
        }
    }
}
