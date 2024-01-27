using FluffyUnderware.Curvy.Generator.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
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
        private static Vector3 pasteRegionStart;
        private static Vector3 pasteRegionEnd;
        private static GameObject pasteRegionDisplayBox;

        // Public Functions

        public static void startPasting() {
            if (BlueprintsPlugin.clipboard == null) {
                Debug.Log("Can't paste null clipboard");
                return;
            }

            if (!BlueprintsPlugin.debugMode) {
                foreach (MachineCost cost in BlueprintsPlugin.clipboard.getCost()) {
                    ResourceInfo info = SaveState.GetResInfoFromId(cost.resId);
                    if (!Player.instance.inventory.myInv.HasResources(info, cost.count)) {
                        Debug.Log($"Not enough {info.displayName} {Player.instance.inventory.myInv.GetResourceCount(cost.resId)}/{cost.count}");
                        return;
                    }
                }

                foreach (MachineCost cost in BlueprintsPlugin.clipboard.getCost()) {
                    Player.instance.inventory.myInv.TryRemoveResources(cost.resId, cost.count);
                }
            }

            pasteRegionStart = Player.instance.builder.CurrentAimTarget;
            isPasting = true;

            FieldInfo takeResourcesBoxInfo = Player.instance.interaction.GetType().GetField("takeResourcesBox", BindingFlags.Instance | BindingFlags.NonPublic);
            GameObject takeResourcesBox = (GameObject)takeResourcesBoxInfo.GetValue(Player.instance.interaction);

            if (pasteRegionDisplayBox == null) pasteRegionDisplayBox = GameObject.Instantiate(takeResourcesBox);
            pasteRegionDisplayBox.SetActive(true);
            updateDisplayBox();
        }

        public static void updateDisplayBox() {
            Vector3Int location = BlueprintsPlugin.getAboveMinPosOfAimedMachine();
            if(BlueprintsPlugin.clipboard.getRotatedSize().x < 0) ++location.x;
            if (BlueprintsPlugin.clipboard.getRotatedSize().z < 0) ++location.z;

            pasteRegionDisplayBox.transform.localScale = BlueprintsPlugin.clipboard.getRotatedSize();
            pasteRegionDisplayBox.transform.position = location;
            pasteRegionDisplayBox.transform.rotation = Quaternion.identity;

            if (BlueprintsPlugin.cwRotateShortcut.Value.IsDown()) {
                BlueprintsPlugin.clipboard.rotateCW();
            }
            else if (BlueprintsPlugin.ccwRotateShortcut.Value.IsDown()) {
                BlueprintsPlugin.clipboard.rotateCCW();
            }

            //Debug.Log($"Rounded Aim Location: {BlueprintsPlugin.getAboveMinPosOfAimedMachine()}");
            //Debug.Log($"Position: {location}");
            //Debug.Log($"Size: {BlueprintsPlugin.clipboard.size}");
        }

        public static void endPasting() {
            isPasting = false;
            pasteRegionDisplayBox.SetActive(false);

            Vector3 placement = Player.instance.builder.CurrentAimTarget;
            Debug.Log($"Pasting clipboard above {placement}");
            Debug.Log($"Rounded: {BlueprintsPlugin.getAboveMinPosOfAimedMachine()}");
            Debug.Log($"Rotated Size: {BlueprintsPlugin.clipboard.getRotatedSize()}");

            List<Vector3> rotatedRelativePositions = BlueprintsPlugin.clipboard.getMachineRelativePositions();

            for (int i = 0; i < BlueprintsPlugin.clipboard.machines.Count; i++) {
                IMachineInstanceRef machine = BlueprintsPlugin.clipboard.machines[i];

                Debug.Log($"Machine {i + 1} ({SaveState.GetResInfoFromId(machine.GetCommonInfo().resId)}) has an offset of {BlueprintsPlugin.clipboard.machineRelativePositions[i]}");

                Vector3Int newMinPos = BlueprintsPlugin.getAboveMinPosOfAimedMachine();
                
                newMinPos.x += (int)rotatedRelativePositions[i].x;
                newMinPos.y += (int)rotatedRelativePositions[i].y;
                newMinPos.z += (int)rotatedRelativePositions[i].z;

                Debug.Log($"Placing machine at {newMinPos}");

                GridInfo newGridInfo = new GridInfo();
                newGridInfo.CopyFrom(machine.gridInfo);
                //Debug.Log($"Machine {machine.instanceId} pre-rotation yawRot: {newGridInfo.YawRotation}");
                int newYaw = machine.gridInfo.yawRot + BlueprintsPlugin.clipboard.getMachineRotation(machine.instanceId);
                if (newYaw >= 360) newYaw -= 360;
                else if (newYaw < 0) newYaw += 360;

                newGridInfo.SetYawRotation(machine.gridInfo.yawRot + BlueprintsPlugin.clipboard.getMachineRotation(machine.instanceId));
                //Debug.Log($"Machine {machine.instanceId} post-rotation yawRot: {newGridInfo.YawRotation}");

                if (machine.typeIndex != MachineTypeEnum.Conveyor) {
                    SimpleBuildInfo info = new SimpleBuildInfo() {
                        machineType = machine.GetCommonInfo().resId,
                        rotation = newGridInfo.YawRotation,
                        minGridPos = newMinPos,
                    };
                    info.tick += 10;
                    SimpleBuildAction action = new SimpleBuildAction() {
                        info = (SimpleBuildInfo)info.Clone(),
                        resourceCostAmount = 1,
                        resourceCostID = machine.GetCommonInfo().resId,
                    };

                    Debug.Log($"Sending build action for {info.machineType} = {SaveState.GetResInfoFromId(info.machineType)}");

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
                    ConveyorMachineList machineList = (ConveyorMachineList)FactorySimManager.instance.machineManager.GetMachineList<ConveyorInstance, ConveyorDefinition>(MachineTypeEnum.Conveyor);
                    List<ConveyorBuildInfo.ChainData> chainData = new List<ConveyorBuildInfo.ChainData>();
                    bool reversed = false;

                    GenericMachineInstanceRef thisGenericMachine = BlueprintsPlugin.clipboard.genericMachineInstanceRefs[i];
                    ConveyorInstance instance = machineList.myArray[thisGenericMachine.index];
                    reversed = instance.buildBackwards;

                    //if (instance.beltShape == ConveyorInstance.BeltShape.Up || instance.beltShape == ConveyorInstance.BeltShape.Down) {
                    //    --newMinPos.y;
                    //}

                    chainData.Add(new ConveyorBuildInfo.ChainData() {
                        count = 1,
                        shape = instance.beltShape,
                        rotation = newGridInfo.yawRot,
                        start = newMinPos
                    });

                    ConveyorBuildInfo info = new ConveyorBuildInfo() {
                        machineType = machine.GetCommonInfo().resId,
                        chainData = chainData,
                        isReversed = reversed,
                        machineIds = new List<uint>(),

                    };
                    info.tick += 5;
                    ConveyorBuildAction action = new ConveyorBuildAction() {
                        info = (ConveyorBuildInfo)info.Clone(),
                        resourceCostAmount = info.chainData.Count,
                        resourceCostID = machine.GetCommonInfo().resId
                    };
                    NetworkMessageRelay.instance.SendNetworkAction(action);
                }
            }

            BlueprintsPlugin.clipboard.clearMachineRotations();
        }
    }
}
