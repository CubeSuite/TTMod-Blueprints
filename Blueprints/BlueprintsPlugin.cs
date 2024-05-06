using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Blueprints.Patches;
using EquinoxsModUtils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using static ConveyorBuildInfo;

namespace Blueprints
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class BlueprintsPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.Blueprints";
        private const string PluginName = "Blueprints";
        private const string VersionString = "3.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        #region Keybinds

        private static string copyShortcutKey = "Copy Shortcut";
        private static string pasteShortcutKey = "Paste Shortcut";
        private static string cancelShortcutKey = "Cancel Shortcut";
        private static string cwRotateKey = "CW Rotation Shortcut";
        private static string ccwRotateKey = "CCW Rotation Shortcut";
        private static string blueprintsKey = "Open Blueprints Shortcut";

        private static string lockPositionKey = "Lock Position";
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

        public static ConfigEntry<KeyboardShortcut> lockPositionShortcut;
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
        
        // Unity Functions

        private void Awake() {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            BindConfigEntries();
            ApplyPatches();

            BlueprintsLibraryGUI.LoadTextures();
            BlueprintManager.LoadData();
            BookManager.LoadData();

            if(BookManager.GetBookCount() == 0) {
                BookManager.AddBook(new BlueprintBook() { name = "All Blueprints" });
            }

            BookManager.currentBookId = 0;

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }

        private void Update() {
            HandleInput();

            if (MachineCopier.isCopying) MachineCopier.UpdateEndPosition();
            if (MachinePaster.isPasting) MachinePaster.updateHolograms();
        }

        float sSinceLastBuild = 0;
        private void FixedUpdate() {
            if(machinesToCopy.Count != 0) {
                IMachineInstanceRef machine = machinesToCopy[0];
                AddMachineToBlueprint(machine);
                machinesToCopy.RemoveAt(0);
                if(machinesToCopy.Count == 0) {
                    Notify("Region copied!");
                }
            }

            sSinceLastBuild += Time.deltaTime;
            for(int i = 0; i < BuildQueue.queuedBuildings.Count; i++) {
                if (ShouldBuild(i)) {
                    BuildQueue.HideHologram(i);

                    List<Vector3Int> invalidCoords = new List<Vector3Int>();
                    if (GridManager.instance.CheckBuildableAt(BuildQueue.queuedBuildings[i].gridInfo, out invalidCoords)) {
                        BuildBuilding(BuildQueue.queuedBuildings[i]);
                    }
                    
                    BuildQueue.queuedBuildings.RemoveAt(i);
                    sSinceLastBuild = 0;
                    break;
                }
            }

            if(BuildQueue.queuedBuildings.Count == 0 && BuildQueue.holograms.Count != 0) {
                BuildQueue.ClearHolograms();
            }
        }

        private void OnGUI() {
            if (BlueprintsLibraryGUI.shouldShow) {
                BlueprintsLibraryGUI.DrawGUI();
            }
            else if (BuildQueue.shouldShowBuildQueue) {
                BuildQueue.ShowBuildQueue();
            }
        }

        // Public Functions

        public static void Notify(string message) {
            Debug.Log(message);
            UIManager.instance.systemLog.FlashMessage(message);
        }

        // Private Functions

        private void BindConfigEntries() {
            copyShortcut = Config.Bind("General", copyShortcutKey, new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl));
            pasteShortcut = Config.Bind("General", pasteShortcutKey, new KeyboardShortcut(KeyCode.V, KeyCode.LeftControl));
            cancelShortcut = Config.Bind("General", cancelShortcutKey, new KeyboardShortcut(KeyCode.Backspace));
            cwRotateShortcut = Config.Bind("General", cwRotateKey, new KeyboardShortcut(KeyCode.Z));
            ccwRotateShortcut = Config.Bind("General", ccwRotateKey, new KeyboardShortcut(KeyCode.Z, KeyCode.LeftControl));
            blueprintsShortcut = Config.Bind("General", blueprintsKey, new KeyboardShortcut(KeyCode.X, KeyCode.LeftControl));

            lockPositionShortcut = Config.Bind("Nudge", lockPositionKey, new KeyboardShortcut(KeyCode.N));
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

        private void ApplyPatches() {
            Harmony.CreateAndPatchAll(typeof(PlayerBuilderPatch));
            Harmony.CreateAndPatchAll(typeof(PlayerInspectorPatch));
            Harmony.CreateAndPatchAll(typeof(PlayerInteractionPatch));
        }

        private void HandleInput() {
            if (copyShortcut.Value.IsDown() && !BlueprintsLibraryGUI.shouldShow && !MachinePaster.isPasting) {
                if (!MachineCopier.isCopying) {
                    MachineCopier.StartCopying();
                }
                else {
                    MachineCopier.EndCopying();
                }
            }

            if (pasteShortcut.Value.IsDown() && !BlueprintsLibraryGUI.shouldShow && !MachineCopier.isCopying) {
                if (!MachinePaster.isPasting) {
                    MachinePaster.startPasting();
                }
                else {
                    MachinePaster.endPasting();
                }
            }

            if (lockPositionShortcut.Value.IsDown() && !BlueprintsLibraryGUI.shouldShow && MachinePaster.isPasting) {
                MachinePaster.isPositionLocked = !MachinePaster.isPositionLocked;   
            }

            if (blueprintsShortcut.Value.IsDown()) {
                BlueprintsLibraryGUI.shouldShow = !BlueprintsLibraryGUI.shouldShow;
                ModUtils.FreeCursor(BlueprintsLibraryGUI.shouldShow);
            }
        }

        private void AddMachineToBlueprint(IMachineInstanceRef machine) {
            bool debugFunction = false;

            clipboard.machineIDs.Add(machine.instanceId);
            clipboard.machineIndexes.Add(machine.index);
            clipboard.machineResIDs.Add(machine.GetCommonInfo().resId);
            clipboard.machineTypes.Add((int)machine.typeIndex);
            clipboard.machineRotations.Add(machine.GetGridInfo().yawRot);
            clipboard.machineDimensions.Add(GetDimensions(machine).ToString());
            clipboard.machineVariationIndexes.Add(machine.GetCommonInfo().variationIndex);

            if (debugFunction) {
                string resName = SaveState.GetResInfoFromId(machine.GetCommonInfo().resId).displayName;

                Debug.Log($"addMachineToBlueprint() machine.instanceId: {machine.instanceId}");
                Debug.Log($"addMachineToBlueprint() machine.index: {machine.index}");
                Debug.Log($"addMachineToBlueprint() machine.resID: {machine.GetCommonInfo().resId}");
                Debug.Log($"addMachineToBlueprint() machine.resName: {resName}");
                Debug.Log($"addMachineToBlueprint() machine.typIndex: {machine.typeIndex}");
                Debug.Log($"addMachineToBlueprint() machine.yawRot: {machine.GetGridInfo().yawRot}");
                Debug.Log($"addMachineToBlueprint() machine.dims: {machine.gridInfo.dims}");
                Debug.Log($"addMachineToBlueprint() machine.variationIndex: {machine.GetCommonInfo().variationIndex}");
            }

            GenericMachineInstanceRef generic = machine.AsGeneric();

            switch (machine.typeIndex) {
                case MachineTypeEnum.Assembler:
                    AssemblerInstance assembler = generic.Get<AssemblerInstance>();
                    clipboard.machineRecipes.Add(assembler.targetRecipe == null ? -1 : assembler.targetRecipe.uniqueId);
                    clipboard.conveyorShapes.Add(0);
                    clipboard.conveyorBuildBackwards.Add(false);
                    clipboard.conveyorHeights.Add(0);
                    clipboard.conveyorInputBottoms.Add(false);
                    clipboard.conveyorTopYawRots.Add(0);
                    clipboard.chestSizes.Add(0);
                    break;

                case MachineTypeEnum.Inserter:
                    InserterInstance inserter = generic.Get<InserterInstance>();
                    clipboard.machineRecipes.Add(inserter.filterType);
                    clipboard.conveyorShapes.Add(0);
                    clipboard.conveyorBuildBackwards.Add(false);
                    clipboard.conveyorHeights.Add(0);
                    clipboard.conveyorInputBottoms.Add(false);
                    clipboard.conveyorTopYawRots.Add(0);
                    clipboard.chestSizes.Add(0);
                    break;

                case MachineTypeEnum.Conveyor:
                    ConveyorInstance conveyor = generic.Get<ConveyorInstance>();
                    clipboard.machineRecipes.Add(0);
                    clipboard.conveyorShapes.Add((int)conveyor.beltShape);
                    clipboard.conveyorBuildBackwards.Add(conveyor.buildBackwards);
                    clipboard.conveyorHeights.Add(conveyor.verticalHeight);
                    clipboard.conveyorInputBottoms.Add(conveyor.inputBottom);
                    clipboard.conveyorTopYawRots.Add(conveyor.topYawRot);
                    clipboard.chestSizes.Add(0);
                    break;

                case MachineTypeEnum.Chest:
                    ChestInstance chest = generic.Get<ChestInstance>();
                    clipboard.machineRecipes.Add(0);
                    clipboard.conveyorShapes.Add(0);
                    clipboard.conveyorBuildBackwards.Add(false);
                    clipboard.conveyorHeights.Add(0);
                    clipboard.conveyorInputBottoms.Add(false);
                    clipboard.conveyorTopYawRots.Add(0);
                    clipboard.chestSizes.Add(chest.commonInfo.inventories[0].numSlots);
                    break;

                default:
                    clipboard.machineRecipes.Add(0);
                    clipboard.conveyorShapes.Add(0);
                    clipboard.conveyorBuildBackwards.Add(false);
                    clipboard.conveyorHeights.Add(0);
                    clipboard.conveyorInputBottoms.Add(false);
                    clipboard.conveyorTopYawRots.Add(0);
                    clipboard.chestSizes.Add(0);
                    break;
            }
        }

        private MyVector3 GetDimensions(IMachineInstanceRef machine) {
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

        private bool ShouldBuild(int index) {
            ResourceInfo info = SaveState.GetResInfoFromId(BuildQueue.queuedBuildings[index].resID);
            return sSinceLastBuild > /*0.25*/ 0 &&
                   Player.instance.inventory.HasResources(info, 1);
            // ToDo: Re-enabled build delay if same ID bug is observed.
        }

        private void BuildBuilding(QueuedBuilding building) {
            bool debugFunction = false;
            MachineTypeEnum type = (MachineTypeEnum)building.type;
            GridInfo gridInfo = building.gridInfo;
            ChainData chainData = new ChainData() {
                count = 1,
                height = building.conveyorHeight,
                shape = (ConveyorInstance.BeltShape)building.conveyorShape,
                rotation = gridInfo.yawRot,
                start = gridInfo.minPos,
                inputBottom = building.conveyorInputBottom,
                topYawRot = building.conveyorTopYawRot
            };

            if (debugFunction) {
                Debug.Log($"id: {building.machineID}");
                Debug.Log($"resID: {building.resID}");
                Debug.Log($"type: {type}");
                Debug.Log($"yawRotation: {gridInfo.yawRot}");
                Debug.Log($"recipe: {building.recipe}");
                Debug.Log($"gridInfo.minPos: {gridInfo.minPos}");
                Debug.Log($"buildBackwards: {building.conveyorBuildBackwards}");
            }

            switch (type) {
                case MachineTypeEnum.Accumulator:
                case MachineTypeEnum.Beacon:
                case MachineTypeEnum.BlastSmelter:
                case MachineTypeEnum.Chest:
                case MachineTypeEnum.Drill:
                case MachineTypeEnum.Floor:
                case MachineTypeEnum.LightSticks:
                case MachineTypeEnum.Planter:
                case MachineTypeEnum.ResearchCore:
                case MachineTypeEnum.Smelter:
                case MachineTypeEnum.Stairs:
                case MachineTypeEnum.Thresher:
                case MachineTypeEnum.TransitDepot:
                case MachineTypeEnum.TransitPole:
                case MachineTypeEnum.VoltageStepper:
                    ModUtils.BuildMachine(building.resID, gridInfo, false);
                    break;

                case MachineTypeEnum.Structure:
                    ModUtils.BuildMachine(building.resID, gridInfo, false, building.variationIndex);
                    break;

                case MachineTypeEnum.Assembler:
                case MachineTypeEnum.Inserter:
                    ModUtils.BuildMachine(building.resID, gridInfo, false, -1, building.recipe); break;

                case MachineTypeEnum.Conveyor:
                    ModUtils.BuildMachine(building.resID, gridInfo, false, -1, -1, chainData, building.conveyorBuildBackwards); break;

                default:
                    Debug.Log($"Unsupported Machine type");
                    break;
            }

            sSinceLastBuild = 0;
        }
    }
}
