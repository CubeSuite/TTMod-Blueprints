using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints
{
    public static class MachineCopier
    {
        // Objects & Variables
        public static bool isCopying = false;
        
        private static Vector3 copyRegionStart;
        private static Vector3 copyRegionEnd;
        private static Vector3Int copyRegionAnchor;
        private static Bounds copyRegionBounds;
        private static GameObject copyRegionDisplayBox;

        // Public Functions

        public static void startCopying() {
            Vector3? startPosResult = AimingHelper.getLookedAtMachinePos();
            if (startPosResult == null) return;
            copyRegionStart = (Vector3)startPosResult;
            if (Mathf.Abs(copyRegionStart.y % 1) == 0.5) copyRegionStart.y -= 0.49f;
            //copyRegionStart = BlueprintsPlugin.getMinPosOfAimedMachine();
            //copyRegionStart.y += 0.01f;
            copyRegionBounds = new Bounds(copyRegionStart, Vector3.zero);
            copyRegionAnchor = AimingHelper.getMinPosOfAimedMachine();
            isCopying = true;

            Debug.Log($"Started copying: {copyRegionStart}");

            FieldInfo takeResourcesBoxInfo = Player.instance.interaction.GetType().GetField("takeResourcesBox", BindingFlags.Instance | BindingFlags.NonPublic);
            GameObject takeResourcesBox = (GameObject)takeResourcesBoxInfo.GetValue(Player.instance.interaction);

            if (copyRegionDisplayBox == null) copyRegionDisplayBox = GameObject.Instantiate(takeResourcesBox);
            copyRegionDisplayBox.SetActive(true);
            updateDisplayBox();
        }

        public static void updateEndPosition() {
            Vector3? currentEndPosResult = AimingHelper.getLookedAtMachinePos();
            if (currentEndPosResult == null) return;

            Vector3 currentEndPos = (Vector3)currentEndPosResult;
            Vector3 size = currentEndPos - copyRegionStart;

            copyRegionBounds = new Bounds(copyRegionStart, Vector3.zero);
            copyRegionBounds.Encapsulate(copyRegionStart + size);

            HashSet<GenericMachineInstanceRef> machines = getMachineRefsInBounds();
            foreach (GenericMachineInstanceRef machine in machines) {
                copyRegionBounds.EncapsulateMachine(machine);
            }

            updateDisplayBox();
        }

        public static void endCopying() {
            Vector3? endtPosResult = AimingHelper.getLookedAtMachinePos();
            if (endtPosResult == null) return;
            copyRegionEnd = (Vector3)endtPosResult;
            isCopying = false;

            Vector3 size = copyRegionEnd - copyRegionStart;
            copyRegionBounds.Encapsulate(copyRegionStart + size);

            size.x = size.x >= 0 ? copyRegionBounds.size.x : -copyRegionBounds.size.x;
            size.y = copyRegionBounds.size.y;
            size.z = size.z >= 0 ? copyRegionBounds.size.z : -copyRegionBounds.size.z;

            Debug.Log($"Copy Region Start: {copyRegionStart}");
            Debug.Log($"Copy Region End: {copyRegionEnd}");
            Debug.Log($"Size: {size}");
            Debug.Log($"Bounds.size: {copyRegionBounds.size}");

            copyRegionDisplayBox.SetActive(false);

            HashSet<IMachineInstanceRef> machines = getMachinesToCopy();
            Blueprint blueprint = new Blueprint() { name = "test" };

            foreach(IMachineInstanceRef machine in machines) {
                blueprint.machineIDs.Add(machine.instanceId);
                blueprint.machineResIDs.Add(machine.GetCommonInfo().resId);
                blueprint.machineTypes.Add((int)machine.typeIndex);
                blueprint.machineRotations.Add(machine.GetGridInfo().yawRot);

                GenericMachineInstanceRef generic = machine.AsGeneric();

                switch (machine.typeIndex) {
                    case MachineTypeEnum.Assembler:
                        AssemblerInstance assembler = generic.Get<AssemblerInstance>();
                        blueprint.machineRecipes.Add(assembler.targetRecipe == null ? -1 : assembler.targetRecipe.uniqueId);
                        blueprint.conveyorShapes.Add(0);
                        blueprint.conveyorBuildBackwards.Add(false);
                        blueprint.chestSizes.Add(0);
                        break;

                    case MachineTypeEnum.Inserter:
                        InserterInstance inserter = generic.Get<InserterInstance>();
                        blueprint.machineRecipes.Add(inserter.filterType);
                        blueprint.conveyorShapes.Add(0);
                        blueprint.conveyorBuildBackwards.Add(false);
                        blueprint.chestSizes.Add(0);
                        break;

                    case MachineTypeEnum.Conveyor:
                        ConveyorInstance conveyor = generic.Get<ConveyorInstance>();
                        blueprint.machineRecipes.Add(0);
                        blueprint.conveyorShapes.Add((int)conveyor.beltShape);
                        blueprint.conveyorBuildBackwards.Add(conveyor.buildBackwards);
                        blueprint.chestSizes.Add(0);
                        break;

                    case MachineTypeEnum.Chest:
                        ChestInstance chest = generic.Get<ChestInstance>();
                        blueprint.machineRecipes.Add(0);
                        blueprint.conveyorShapes.Add(0);
                        blueprint.conveyorBuildBackwards.Add(false);
                        blueprint.chestSizes.Add(chest.commonInfo.inventories[0].numSlots);
                        break;

                    default:
                        blueprint.machineRecipes.Add(0);
                        blueprint.conveyorShapes.Add(0);
                        blueprint.conveyorBuildBackwards.Add(false);
                        blueprint.chestSizes.Add(0);
                        break;
                }
            }

            blueprint.setSize(size);

            //Vector3Int blueprintAnchorPoint = new Vector3Int() {
            //    x = (int)(copyRegionStart.x >= 0 ? Math.Floor(copyRegionStart.x) : Math.Ceiling(copyRegionStart.x)),
            //    y = (int)(copyRegionStart.y >= 0 ? Math.Floor(copyRegionStart.y) : Math.Ceiling(copyRegionStart.y)),
            //    z = (int)(copyRegionStart.z >= 0 ? Math.Floor(copyRegionStart.z) : Math.Ceiling(copyRegionStart.z)),
            //};

            foreach(IMachineInstanceRef machine in machines) {
                blueprint.machineRelativePositions.Add(new MyVector3(machine.GetGridInfo().minPos - copyRegionAnchor));
                //Debug.Log($"minPos({machine.GetGridInfo().minPos}) - anchor({copyRegionAnchor}) = offset({machine.GetGridInfo().minPos - copyRegionAnchor})");
            }

            BlueprintsPlugin.clipboard = blueprint;
            BlueprintsPlugin.saveClipboardToFile();
        }

        // Private Functions

        private static void updateDisplayBox() {
            copyRegionDisplayBox.transform.localScale = copyRegionBounds.size;
            copyRegionDisplayBox.transform.position = copyRegionBounds.min;
            copyRegionDisplayBox.transform.rotation = Quaternion.identity;
        }

        private static HashSet<GenericMachineInstanceRef> getMachineRefsInBounds() {
            HashSet<GenericMachineInstanceRef> machines = new HashSet<GenericMachineInstanceRef>();
            Collider[] colliders = Physics.OverlapBox(copyRegionBounds.center, copyRegionBounds.extents, Quaternion.identity, (LayerMask)AimingHelper.buildablesMask);
            foreach (Collider collider in colliders) {
                GenericMachineInstanceRef collidedMachineRef = FHG_Utils.FindMachineRef(collider.gameObject);
                if (collidedMachineRef.IsValid()) {
                    machines.Add(collidedMachineRef);
                }
            }

            return machines;
        }

        private static HashSet<IMachineInstanceRef> getMachinesToCopy() {
            HashSet<GenericMachineInstanceRef> machineRefs = getMachineRefsInBounds();
            HashSet<IMachineInstanceRef> machines = new HashSet<IMachineInstanceRef>();
            foreach (GenericMachineInstanceRef machineRef in machineRefs) {
                if (machineRef.IsValid()) {
                    machines.Add(machineRef.CastToInterface());
                }
            }

            return machines;
        }
    }
}
