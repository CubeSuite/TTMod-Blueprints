using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator.Modules;
using Mirror;
using Newtonsoft.Json;
using ProceduralNoiseProject;
using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Blueprints
{
    public static class MachinePaster
    {
        // Objects & Variables
        public static bool isPasting = false;
        
        private static Blueprint clipboard => BlueprintsPlugin.clipboard;
        private static List<Vector3> rotatedRelativePositions;
        private static List<StreamedHologramData> holograms = new List<StreamedHologramData>();
        private static Vector3 nudgeOffset;

        // Public Functions

        public static void startPasting() {
            bool debugFunction = false;

            BlueprintsPlugin.loadFileToClipboard();
            if (clipboard == null) {
                BlueprintsPlugin.Notify("Nothing to paste!");
                return;
            }

            if (!checkInventory()) return;

            rotatedRelativePositions = clipboard.getMachineRelativePositions();
            isPasting = true;
            nudgeOffset = new Vector3();

            renderHolograms();
        }

        public static void updateHolograms() {
            if (BlueprintsPlugin.cancelShortcut.Value.IsDown()) {
                hideHolograms();
                isPasting = false;
                BlueprintsPlugin.Notify("Canceled pasting");
            }
            
            Vector3 currentAim = AimingHelper.getAimedLocationForPasting();
            Vector3Int blueprintSize = clipboard.getRotatedSize();
            if (blueprintSize.x % 2 == 0) currentAim.x += 0.5f;
            if (blueprintSize.z % 2 == 0) currentAim.z += 0.5f;

            checkForNudging();
            currentAim += nudgeOffset;

            for (int i = 0; i < holograms.Count; i++) {
                Vector3 newLocation = currentAim + rotatedRelativePositions[i];

                int newYaw = clipboard.machineRotations[i] + clipboard.getMachineRotation(clipboard.machineIDs[i]);
                newYaw = newYaw % 360;
                if (newYaw < 0) newYaw += 360;

                holograms[i].SetTransform(newLocation, Quaternion.Euler(0, newYaw, 0));
            }

            if (BlueprintsPlugin.cwRotateShortcut.Value.IsDown() && !UI.isOpen) {
                clipboard.rotateCW();
                rotatedRelativePositions = clipboard.getMachineRelativePositions();
            }
            else if (BlueprintsPlugin.ccwRotateShortcut.Value.IsDown() && !UI.isOpen) {
                clipboard.rotateCCW();
                rotatedRelativePositions = clipboard.getMachineRelativePositions();
            }
        }

        public static void endPasting() {
            isPasting = false;
            
            for (int i = 0; i < clipboard.machineIDs.Count; i++) {
                uint id = clipboard.machineIDs[i];
                int resId = clipboard.machineResIDs[i];
                MachineTypeEnum type = (MachineTypeEnum)clipboard.machineTypes[i];
                int yawRotation = clipboard.machineRotations[i];
                int recipe = clipboard.machineRecipes[i];
                ConveyorInstance.BeltShape beltShape = (ConveyorInstance.BeltShape)clipboard.conveyorShapes[i];
                bool buildBeltBackwards = clipboard.conveyorBuildBackwards[i];

                GridInfo newGridInfo = getNewGridInfo(rotatedRelativePositions, i, yawRotation, id);
                if (!GridManager.instance.CheckBuildableIncludingVoxelsAt(newGridInfo)) {
                    BlueprintsPlugin.Notify($"Couldn't build {type} at {newGridInfo.minPos}");
                    continue;
                }

                switch (type) {
                    case MachineTypeEnum.Assembler:
                        if (recipe == -1) doSimpleBuild(resId, type, newGridInfo);
                        else doSimpleWithRecipeBuild(resId, type, newGridInfo, recipe);
                        break;

                    case MachineTypeEnum.Inserter: 
                        doSimpleWithRecipeBuild(resId, type, newGridInfo, recipe); 
                        break;

                    case MachineTypeEnum.Conveyor:
                        List<ConveyorBuildInfo.ChainData> chainData = new List<ConveyorBuildInfo.ChainData>() {
                            new ConveyorBuildInfo.ChainData() {
                                count = 1,
                                shape = beltShape,
                                rotation = newGridInfo.yawRot,
                                start = newGridInfo.minPos
                            }
                        };
                        doConveyorBuild(resId, chainData, buildBeltBackwards);
                        break;

                    case MachineTypeEnum.Drill:
                        doDrillBuild(resId, newGridInfo);
                        break;

                    case MachineTypeEnum.Smelter:
                        doSmelterBuild(resId, newGridInfo, i);
                        break;

                    default:
                        doSimpleBuild(resId, type, newGridInfo);
                        break;
                }
            }

            clipboard.rotation = 0;
            clipboard.clearMachineRotations();

            if(BlueprintsPlugin.machinesToBuild.Count == 0) {
                hideHolograms();
            }
        }

        public static void hideHolograms() {
            foreach (StreamedHologramData hologram in holograms) {
                hologram.AbandonHologramPreview();
            }

            holograms.Clear();
        }

        // Private Functions

        private static bool checkInventory() {
            bool hasResources = true;
            foreach (MachineCost cost in clipboard.getCost()) {
                ResourceInfo info = SaveState.GetResInfoFromId(cost.resId);
                if (!Player.instance.inventory.myInv.HasResources(info, cost.count)) {
                    BlueprintsPlugin.Notify($"Not enough {info.displayName} {Player.instance.inventory.myInv.GetResourceCount(cost.resId)}/{cost.count}");
                    hasResources = false;
                }
            }

            return hasResources;
        }

        private static void renderHolograms() {
            Vector3 location = AimingHelper.getAimedLocationForPasting();

            Vector3Int blueprintSize = clipboard.getRotatedSize();
            if (blueprintSize.x % 2 == 0) location.x += 0.5f;
            if (blueprintSize.z % 2 == 0) location.z += 0.5f;

            for (int i = 0; i < clipboard.machineIDs.Count; i++) {
                int machineIndex = clipboard.machineIndexes[i];
                MachineTypeEnum type = (MachineTypeEnum)clipboard.machineTypes[i];
                Vector3 offset = rotatedRelativePositions[i];
                float yawRotation = 0;

                Vector3 thisHologramPos = location + offset;
                StreamedHologramData hologram = null;

                if(type == MachineTypeEnum.Conveyor) {
                    ConveyorInstance conveyor = MachineManager.instance.Get<ConveyorInstance, ConveyorDefinition>(machineIndex, type);
                    ConveyorHologramData conveyorHologram = conveyor.myDef.GenerateUnbuiltHologramData() as ConveyorHologramData;
                    conveyorHologram.buildBackwards = conveyor.buildBackwards;
                    conveyorHologram.curShape = conveyor.beltShape;
                    conveyorHologram.numBelts = 1;

                    thisHologramPos.x += conveyor.gridInfo.dims.x / 2.0f;
                    thisHologramPos.z += conveyor.gridInfo.dims.z / 2.0f;
                    yawRotation = conveyor.gridInfo.yawRot;

                    Quaternion conveyorRotation = Quaternion.Euler(0, yawRotation, 0);
                    conveyorHologram.SetTransform(thisHologramPos, conveyorRotation);
                    conveyorHologram.ShowUnbuilt(true, true);
                    holograms.Add(conveyorHologram);
                    continue;
                }

                switch (type) {
                    case MachineTypeEnum.Assembler:
                        AssemblerInstance assembler = MachineManager.instance.Get<AssemblerInstance, AssemblerDefinition>(machineIndex, type);
                        hologram = assembler.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += assembler.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += assembler.gridInfo.dims.z / 2.0f;
                        yawRotation = assembler.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Chest:
                        ChestInstance chest = MachineManager.instance.Get<ChestInstance, ChestDefinition>(machineIndex, type);
                        hologram = chest.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += chest.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += chest.gridInfo.dims.z / 2.0f;
                        yawRotation = chest.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Drill:
                        DrillInstance drill = MachineManager.instance.Get<DrillInstance, DrillDefinition>(machineIndex, type);
                        hologram = drill.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += drill.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += drill.gridInfo.dims.z / 2.0f;
                        yawRotation = drill.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Floor:
                        FloorInstance floor = MachineManager.instance.Get<FloorInstance, FloorDefinition>(machineIndex, type);
                        hologram = floor.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += floor.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += floor.gridInfo.dims.z / 2.0f;
                        yawRotation = floor.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Inserter:
                        InserterInstance inserter = MachineManager.instance.Get<InserterInstance, InserterDefinition>(machineIndex, type);
                        hologram = inserter.myDef.GenerateUnbuiltHologramData();
                        yawRotation = inserter.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.LightSticks:
                        LightStickInstance lightStick = MachineManager.instance.Get<LightStickInstance, LightStickDefinition>(machineIndex, type);
                        hologram = lightStick.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += lightStick.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += lightStick.gridInfo.dims.z / 2.0f;
                        yawRotation = lightStick.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Planter:
                        PlanterInstance planter = MachineManager.instance.Get<PlanterInstance, PlanterDefinition>(machineIndex, type);
                        hologram = planter.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += planter.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += planter.gridInfo.dims.z / 2.0f;
                        yawRotation = planter.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.PowerGenerator:
                        PowerGeneratorInstance generator = MachineManager.instance.Get<PowerGeneratorInstance, PowerGeneratorDefinition>(machineIndex, type);
                        hologram = generator.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += generator.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += generator.gridInfo.dims.z / 2.0f;
                        yawRotation = generator.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.ResearchCore:
                        ResearchCoreInstance researchCore = MachineManager.instance.Get<ResearchCoreInstance, ResearchCoreDefinition>(machineIndex, type);
                        hologram = researchCore.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += researchCore.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += researchCore.gridInfo.dims.z / 2.0f;
                        yawRotation = researchCore.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Smelter:
                        SmelterInstance smelter = MachineManager.instance.Get<SmelterInstance, SmelterDefinition>(machineIndex, type);
                        hologram = smelter.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += smelter.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += smelter.gridInfo.dims.z / 2.0f;
                        yawRotation = smelter.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Stairs:
                        StairsInstance stairs = MachineManager.instance.Get<StairsInstance, StairsDefinition>(machineIndex, type);
                        hologram = stairs.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += stairs.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += stairs.gridInfo.dims.z / 2.0f;
                        yawRotation = stairs.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Thresher:
                        ThresherInstance thresher = MachineManager.instance.Get<ThresherInstance, ThresherDefinition>(machineIndex, type);
                        hologram = thresher.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += thresher.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += thresher.gridInfo.dims.z / 2.0f;
                        yawRotation = thresher.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.TransitDepot:
                        TransitDepotInstance transitDepot = MachineManager.instance.Get<TransitDepotInstance, TransitDepotDefinition>(machineIndex, type);
                        hologram = transitDepot.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += transitDepot.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += transitDepot.gridInfo.dims.z / 2.0f;
                        yawRotation = transitDepot.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.TransitPole:
                        TransitPoleInstance transitPole = MachineManager.instance.Get<TransitPoleInstance, TransitPoleDefinition>(machineIndex, type);
                        hologram = transitPole.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += transitPole.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += transitPole.gridInfo.dims.z / 2.0f;
                        yawRotation = transitPole.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.WaterWheel:
                        WaterWheelInstance waterWheel = MachineManager.instance.Get<WaterWheelInstance, WaterWheelDefinition>(machineIndex, type);
                        hologram = waterWheel.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += waterWheel.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += waterWheel.gridInfo.dims.z / 2.0f;
                        yawRotation = waterWheel.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Accumulator:
                        AccumulatorInstance accumulator = MachineManager.instance.Get<AccumulatorInstance, AccumulatorDefinition>(machineIndex, type);
                        hologram = accumulator.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += accumulator.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += accumulator.gridInfo.dims.z / 2.0f;
                        yawRotation = accumulator.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.HighVoltageCable:
                        HighVoltageCableInstance hvc = MachineManager.instance.Get<HighVoltageCableInstance, HighVoltageCableDefinition>(machineIndex, type);
                        hologram = hvc.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += hvc.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += hvc.gridInfo.dims.z / 2.0f;
                        yawRotation = hvc.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.VoltageStepper:
                        VoltageStepperInstance stepper = MachineManager.instance.Get<VoltageStepperInstance, VoltageStepperDefinition>(machineIndex, type);
                        hologram = stepper.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += stepper.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += stepper.gridInfo.dims.z / 2.0f;
                        yawRotation = stepper.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.Structure:
                        StructureInstance structure = MachineManager.instance.Get<StructureInstance, StructureDefinition>(machineIndex, type);
                        hologram = structure.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += structure.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += structure.gridInfo.dims.z / 2.0f;
                        yawRotation = structure.gridInfo.yawRot;
                        break;

                    case MachineTypeEnum.BlastSmelter:
                        BlastSmelterInstance blastSmelter = MachineManager.instance.Get<BlastSmelterInstance, BlastSmelterDefinition>(machineIndex, type);
                        hologram = blastSmelter.myDef.GenerateUnbuiltHologramData();
                        thisHologramPos.x += blastSmelter.gridInfo.dims.x / 2.0f;
                        thisHologramPos.z += blastSmelter.gridInfo.dims.z / 2.0f;
                        yawRotation = blastSmelter.gridInfo.yawRot;
                        break;

                    default:
                        Debug.Log($"Skipped rendering hologram for unknown type: {type}");
                        continue;
                }

                Quaternion rotation = Quaternion.Euler(0, yawRotation, 0);
                hologram.SetTransform(thisHologramPos, rotation);
                hologram.ShowUnbuilt(true, true);
                holograms.Add(hologram);
            }
        }

        private static void checkForNudging() {
            Vector3 camFacing = Player.instance.cam.transform.forward;
            Vector3 nudgeDirection = Vector3.zero;

            Vector3 left = -Vector3.Cross(Vector3.up, camFacing).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, camFacing).normalized;
            Vector3 forward = camFacing;
            Vector3 backward = -camFacing;
            forward.y = 0;
            backward.y = 0;

            if (BlueprintsPlugin.nudgeLeftShortcut.Value.IsDown()) nudgeDirection = AimingHelper.clampToAxis(left);
            else if (BlueprintsPlugin.nudgeRightShortcut.Value.IsDown()) nudgeDirection = AimingHelper.clampToAxis(right);
            else if (BlueprintsPlugin.nudgeForwardShortcut.Value.IsDown()) nudgeDirection = AimingHelper.clampToAxis(forward);
            else if (BlueprintsPlugin.nudgeBackwardShortcut.Value.IsDown()) nudgeDirection = AimingHelper.clampToAxis(backward);
            else if (BlueprintsPlugin.nudgeUpShortcut.Value.IsDown()) nudgeDirection = Vector3.up;
            else if (BlueprintsPlugin.nudgeDownShortcut.Value.IsDown()) nudgeDirection = Vector3.down;

            if(nudgeDirection != Vector3.zero) {
                nudgeOffset += nudgeDirection;
            }
        }

        private static GridInfo getNewGridInfo(List<Vector3> rotatedRelativePositions, int index, int yawRotation, uint id) {
            bool debugFunction = false;
            
            GridInfo newGridInfo = new GridInfo();

            Vector3 aimLocation = AimingHelper.getAimedLocationForPasting();
            if (debugFunction) Debug.Log($"getNewGridInfo() aimLocation before nudge: {aimLocation}");

            aimLocation += nudgeOffset;
            if (debugFunction) Debug.Log($"getNewGridInfo() nudgeOffset: {nudgeOffset}");
            if (debugFunction) Debug.Log($"getNewGridInfo() aimLocation after nudge: {aimLocation}");
            
            Vector3Int blueprintSize = clipboard.getRotatedSize();
            if (debugFunction) Debug.Log($"getNewGridInfo() blueprintSize: {blueprintSize}");

            if (blueprintSize.x % 2 == 0) aimLocation.x += 0.5f;
            if (blueprintSize.z % 2 == 0) aimLocation.z += 0.5f;
            if (debugFunction) Debug.Log($"getNewGridInfo() aimLocation after size offset: {aimLocation}");

            Vector3 dimensions = clipboard.getMachineDimensions(index);
            Vector3 newMachineCenter = aimLocation + rotatedRelativePositions[index];
            Vector3 newMinPos = new Vector3() {
                x = newMachineCenter.x - dimensions.x / 2.0f,
                y = newMachineCenter.y,
                z = newMachineCenter.z - dimensions.z / 2.0f
            };
            Vector3Int newMinPosInts = new Vector3Int() {
                x = Mathf.RoundToInt(newMinPos.x),
                y = Mathf.RoundToInt(newMinPos.y),
                z = Mathf.RoundToInt(newMinPos.z),
            };
            newGridInfo.minPos = newMinPosInts;

            if (debugFunction) {
                Debug.Log($"getNewGridInfo() machine dimensions: {dimensions}");
                Debug.Log($"getNewGridInfo() newMachineenter: {newMachineCenter}");
                Debug.Log($"getNewGridInfo() newMinPos: {newMinPos}");
                Debug.Log($"getNewGridInfo() newMinPosInts: {newMinPosInts}");
            }

            int newYaw = yawRotation + clipboard.getMachineRotation(id);
            if (newYaw >= 360) newYaw -= 360;
            else if (newYaw < 0) newYaw += 360;
            newGridInfo.SetYawRotation(newYaw);

            return newGridInfo;
        }

        private static void doSimpleBuild(int resId, MachineTypeEnum type, GridInfo newGridInfo) {
            SimpleBuildInfo info = new SimpleBuildInfo() {
                machineType = resId,
                rotation = newGridInfo.YawRotation,
                minGridPos = newGridInfo.minPos,
            };
            SimpleBuildAction action = new SimpleBuildAction() {
                info = (SimpleBuildInfo)info.Clone(),
                resourceCostAmount = 1,
                resourceCostID = resId,
            };
            BlueprintsPlugin.machinesToBuild.Add(action);
        }

        private static void doSimpleWithRecipeBuild(int resId, MachineTypeEnum type, GridInfo newGridInfo, int recipe) {
            BuildWithRecipeInfo info = new BuildWithRecipeInfo() {
                machineType = resId,
                rotation = newGridInfo.YawRotation,
                minGridPos = newGridInfo.minPos,
                recipe = recipe
            };
            BuildWithRecipeAction action = new BuildWithRecipeAction() {
                info = (BuildWithRecipeInfo)info.Clone(),
                resourceCostAmount = 1,
                resourceCostID = resId
            };
            BlueprintsPlugin.machinesToBuild.Add(action);
        }

        private static void doConveyorBuild(int resId, List<ConveyorBuildInfo.ChainData> chainData, bool reversed) {
            ConveyorBuildInfo info = new ConveyorBuildInfo() {
                machineType = resId,
                chainData = chainData,
                isReversed = reversed,
                machineIds = new List<uint>()
            };
            ConveyorBuildAction action = new ConveyorBuildAction() {
                info = (ConveyorBuildInfo)info.Clone(),
                resourceCostAmount = info.chainData.Count,
                resourceCostID = resId
            };
            BlueprintsPlugin.machinesToBuild.Add(action);
        }

        private static void doDrillBuild(int resId, GridInfo newGridInfo) {
            SimpleBuildInfo info = new SimpleBuildInfo() {
                machineType = resId,
                rotation = newGridInfo.YawRotation,
                minGridPos = newGridInfo.minPos
            };
            Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.DrillBuilder).BuildFromNetworkData(info, false);
        }

        private static void doSmelterBuild(int resId, GridInfo newGridInfo, int index) {
            SimpleBuildInfo info = new SimpleBuildInfo() {
                machineType = resId,
                rotation = newGridInfo.yawRot,
                minGridPos = newGridInfo.minPos
            };

            BuilderInfo builderInfo = SaveState.GetResInfoFromId(resId) as BuilderInfo;

            MachineInstanceDefaultBuilder builder = (MachineInstanceDefaultBuilder)Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.MachineInstanceDefaultBuilder);
            builder.newBuildInfo = info;
            builder.curBuilderInfo = builderInfo;
            builder.myNewGridInfo = newGridInfo;
            builder.myHolo = holograms[index];
            builder.recentlyBuilt = true;
            builder.OnShow();

            FieldInfo currentBuilderField = typeof(PlayerBuilder).GetField("_currentBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
            currentBuilderField.SetValue(Player.instance.builder, builder);
            
            FieldInfo lastBuilderField = typeof(PlayerBuilder).GetField("_lastBuilderInfo", BindingFlags.Instance | BindingFlags.NonPublic);
            lastBuilderField.SetValue(Player.instance.builder, builderInfo);

            FieldInfo lastBuildPosField = typeof(PlayerBuilder).GetField("_lastBuildPos", BindingFlags.Instance | BindingFlags.NonPublic);
            lastBuildPosField.SetValue(Player.instance.builder, builder.curGridPlacement.MinInt);
            BuildMachineAction action = builder.GenerateNetworkData();
            action.resourceCostID = builderInfo.uniqueId;
            action.resourceCostAmount = 1;
            NetworkMessageRelay.instance.SendNetworkAction(action);
        }
    }
}
