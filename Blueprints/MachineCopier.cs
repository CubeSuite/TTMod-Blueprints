using System;
using System.Collections.Generic;
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
        private static Bounds copyRegionBounds;
        private static GameObject copyRegionDisplayBox = new GameObject();

        // Public Functions

        public static void startCopying() {
            Vector3? startPosResult = BlueprintsPlugin.getLookedAtMachinePos();
            if (startPosResult == null) return;
            copyRegionStart = (Vector3)startPosResult;
            copyRegionBounds = new Bounds(copyRegionStart, Vector3.zero);

            isCopying = true;

            Debug.Log($"Started copying: {copyRegionStart}");

            FieldInfo takeResourcesBoxInfo = Player.instance.interaction.GetType().GetField("takeResourcesBox", BindingFlags.Instance | BindingFlags.NonPublic);
            GameObject takeResourcesBox = (GameObject)takeResourcesBoxInfo.GetValue(Player.instance.interaction);

            if (copyRegionDisplayBox == null) copyRegionDisplayBox = GameObject.Instantiate(takeResourcesBox);
            copyRegionDisplayBox.SetActive(true);
            updateDisplayBox();
        }

        public static void updateEndPosition() {
            Vector3? currentEndPosResult = BlueprintsPlugin.getLookedAtMachinePos();
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
            Vector3? endtPosResult = BlueprintsPlugin.getLookedAtMachinePos();
            if (endtPosResult == null) return;
            copyRegionEnd = (Vector3)endtPosResult;
            isCopying = false;

            Vector3 size = copyRegionEnd - copyRegionStart;
            copyRegionBounds.Encapsulate(copyRegionStart + size);

            copyRegionDisplayBox.SetActive(false);

            HashSet<IMachineInstanceRef> machines = getMachinesToCopy();
            Blueprint blueprint = new Blueprint() {
                name = "test",
                machines = machines.ToList(),
            };

            foreach(IMachineInstanceRef machine in machines) {
                blueprint.machineRelativePositions.Add(machine.GetGridInfo().Center - copyRegionStart);
            }

            BlueprintsPlugin.clipboard = blueprint;
        }

        // Private Functions

        private static void updateDisplayBox() {
            copyRegionDisplayBox.transform.localScale = copyRegionBounds.size;
            copyRegionDisplayBox.transform.position = copyRegionBounds.min;
            copyRegionDisplayBox.transform.rotation = Quaternion.identity;
        }

        private static HashSet<GenericMachineInstanceRef> getMachineRefsInBounds() {
            HashSet<GenericMachineInstanceRef> machines = new HashSet<GenericMachineInstanceRef>();
            Collider[] colliders = Physics.OverlapBox(copyRegionBounds.center, copyRegionBounds.extents, Quaternion.identity, (LayerMask)BlueprintsPlugin.buildablesMask);
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
