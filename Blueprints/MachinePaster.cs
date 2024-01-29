using FluffyUnderware.Curvy.Generator.Modules;
using Newtonsoft.Json;
using ProceduralNoiseProject;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints
{
    public static class MachinePaster
    {
        // Objects & Variables
        public static bool isPasting = false;
        private static GameObject pasteRegionDisplayBox;

        // Public Functions

        public static void startPasting() {
            BlueprintsPlugin.loadFileToClipboard();
            if (BlueprintsPlugin.clipboard == null) {
                Debug.Log("Can't paste null clipboard");
                return;
            }

            if (!checkInventory()) {
                BlueprintsPlugin.Notify("Not enough resources");
                return;
            }

            Debug.Log($"Pasting {BlueprintsPlugin.clipboard.machineIDs.Count} machines");

            isPasting = true;

            FieldInfo takeResourcesBoxInfo = Player.instance.interaction.GetType().GetField("takeResourcesBox", BindingFlags.Instance | BindingFlags.NonPublic);
            GameObject takeResourcesBox = (GameObject)takeResourcesBoxInfo.GetValue(Player.instance.interaction);

            if (pasteRegionDisplayBox == null) pasteRegionDisplayBox = GameObject.Instantiate(takeResourcesBox);
            pasteRegionDisplayBox.SetActive(true);
            updateDisplayBox();
        }

        public static void updateDisplayBox() {
            Vector3Int location = AimingHelper.getAboveMinPosOfAimedMachine();
            if(BlueprintsPlugin.clipboard.getRotatedSize().x < 0) ++location.x;
            if (BlueprintsPlugin.clipboard.getRotatedSize().z < 0) ++location.z;

            pasteRegionDisplayBox.transform.localScale = BlueprintsPlugin.clipboard.getRotatedSize();
            pasteRegionDisplayBox.transform.position = location;
            pasteRegionDisplayBox.transform.rotation = Quaternion.identity;

            if (BlueprintsPlugin.cwRotateShortcut.Value.IsDown() && !UI.isOpen) {
                BlueprintsPlugin.clipboard.rotateCW();
            }
            else if (BlueprintsPlugin.ccwRotateShortcut.Value.IsDown() && !UI.isOpen) {
                BlueprintsPlugin.clipboard.rotateCCW();
            }
        }

        public static void endPasting() {
            isPasting = false;
            pasteRegionDisplayBox.SetActive(false);
            List<Vector3> rotatedRelativePositions = BlueprintsPlugin.clipboard.getMachineRelativePositions();

            for (int i = 0; i < BlueprintsPlugin.clipboard.machineIDs.Count; i++) {

                uint id = BlueprintsPlugin.clipboard.machineIDs[i];
                int resId = BlueprintsPlugin.clipboard.machineResIDs[i];
                MachineTypeEnum type = (MachineTypeEnum)BlueprintsPlugin.clipboard.machineTypes[i];
                int yawRotation = BlueprintsPlugin.clipboard.machineRotations[i];
                int recipe = BlueprintsPlugin.clipboard.machineRecipes[i];
                ConveyorInstance.BeltShape beltShape = (ConveyorInstance.BeltShape)BlueprintsPlugin.clipboard.conveyorShapes[i];
                bool buildBeltBackwards = BlueprintsPlugin.clipboard.conveyorBuildBackwards[i];

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

                    default:
                        doSimpleBuild(resId, type, newGridInfo);
                        break;
                }
            }

            BlueprintsPlugin.clipboard.rotation = 0;
            BlueprintsPlugin.clipboard.clearMachineRotations();
        }

        // Private Functions

        private static bool checkInventory() {
            if (BlueprintsPlugin.debugMode) return true;
            
            bool hasResources = true;
            foreach (MachineCost cost in BlueprintsPlugin.clipboard.getCost()) {
                ResourceInfo info = SaveState.GetResInfoFromId(cost.resId);
                if (!Player.instance.inventory.myInv.HasResources(info, cost.count)) {
                    BlueprintsPlugin.Notify($"Not enough {info.displayName} {Player.instance.inventory.myInv.GetResourceCount(cost.resId)}/{cost.count}");
                    hasResources = false;
                }
            }

            return hasResources;
        }

        private static GridInfo getNewGridInfo(List<Vector3> rotatedRelativePositions, int index, int yawRotation, uint id) {
            GridInfo newGridInfo = new GridInfo();

            Vector3Int newMinPos = AimingHelper.getAboveMinPosOfAimedMachine();
            newMinPos.x += (int)rotatedRelativePositions[index].x;
            newMinPos.y += (int)rotatedRelativePositions[index].y;
            newMinPos.z += (int)rotatedRelativePositions[index].z;
            newGridInfo.minPos = newMinPos;

            int newYaw = yawRotation + BlueprintsPlugin.clipboard.getMachineRotation(id);
            if (newYaw >= 360) newYaw -= 360;
            else if (newYaw < 0) newYaw += 360;
            newGridInfo.SetYawRotation(newYaw);

            Debug.Log($"Old Yaw Rotation: {yawRotation}");
            Debug.Log($"New Yaw Rotation: {newYaw}");

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
            NetworkMessageRelay.instance.SendNetworkAction(action);
            // if (!BlueprintsPlugin.debugMode) Player.instance.inventory.myInv.TryRemoveResources(action.resourceCostID, action.resourceCostAmount);
            Debug.Log($"Built {type} at {newGridInfo.minPos}");
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
            NetworkMessageRelay.instance.SendNetworkAction(action);
            // if (!BlueprintsPlugin.debugMode) Player.instance.inventory.myInv.TryRemoveResources(action.resourceCostID, action.resourceCostAmount);
            Debug.Log($"Built {type} at {newGridInfo.minPos}");
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
            NetworkMessageRelay.instance.SendNetworkAction(action);
            // if(!BlueprintsPlugin.debugMode) Player.instance.inventory.myInv.TryRemoveResources(action.resourceCostID, action.resourceCostAmount);
            Debug.Log($"Built Conveyor at {chainData[0].count}");
        }

        private static void doDrillBuild(int resId, GridInfo newGridInfo) {
            SimpleBuildInfo info = new SimpleBuildInfo() {
                machineType = resId,
                rotation = newGridInfo.YawRotation,
                minGridPos = newGridInfo.minPos
            };
            Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.DrillBuilder).BuildFromNetworkData(info, false);
            Debug.Log($"Built Drill at {newGridInfo.minPos}");
        }
    }
}
