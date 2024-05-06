using FIMSpace.GroundFitter;
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
        private static Vector3 copyRegionAnchor;
        private static Bounds copyRegionBounds;
        private static GameObject copyRegionDisplayBox;

        private static Vector3 copyRegionMinExpansion;
        private static Vector3 copyRegionMaxExpansion;

        // Public Functions

        public static void StartCopying() {
            bool debugFunction = false;

            Vector3? startPosResult = AimingHelper.getLookedAtMachinePos();
            if (startPosResult == null) {
                BlueprintsPlugin.Notify("Aim at a buildable to copy.");
                return;
            }
            
            copyRegionStart = (Vector3)startPosResult;
            if (Mathf.Abs(copyRegionStart.y % 1) == 0.5) copyRegionStart.y -= 0.49f;

            if (debugFunction) Debug.Log($"startCopying() copyRegionStart: {copyRegionStart}");

            InitialiseCopying();
            GetAndShowDisplayBox();
        }

        public static void UpdateEndPosition() {
            bool debugFunction = false;
            
            if (BlueprintsPlugin.cancelShortcut.Value.IsDown()) CancelCopying(true);

            Vector3? currentEndPosResult = AimingHelper.getLookedAtMachinePos();
            if (currentEndPosResult == null) return;
            Vector3 currentEndPos = (Vector3)currentEndPosResult;

            if (debugFunction) Debug.Log($"updateEndPosition() currentEndPos: {currentEndPos}");

            Vector3 size = currentEndPos - copyRegionStart;
            copyRegionBounds = new Bounds(copyRegionStart, Vector3.zero);
            copyRegionBounds.Encapsulate(copyRegionStart + size);

            if (debugFunction) Debug.Log($"updateEndPosition() size: {size}");

            HashSet<GenericMachineInstanceRef> machines = GetMachineRefsInBounds();
            foreach (GenericMachineInstanceRef machine in machines) {
                copyRegionBounds.EncapsulateMachine(machine);
            }

            UpdateExpansion();
            UpdateDisplayBox();
        }

        public static void EndCopying() {
            bool debugFunction = false;
            
            Vector3? endPosResult = AimingHelper.getLookedAtMachinePos();
            if (endPosResult == null) {
                BlueprintsPlugin.Notify("Aim at a buildable to end.");
                return;
            }

            copyRegionEnd = (Vector3)endPosResult;
            CancelCopying(false);

            if (debugFunction) Debug.Log($"endCopying() copyRegionEnd: {copyRegionEnd}");

            Vector3 size = GetFinalCopyRegionValues();

            if (debugFunction) Debug.Log($"endCopying() size: {size}");

            Blueprint blueprint = new Blueprint();
            blueprint.setSize(size);
            BlueprintsPlugin.clipboard = blueprint;

            HashSet<IMachineInstanceRef> machines = GetMachinesToCopy();
            foreach (IMachineInstanceRef machine in machines) {
                BlueprintsPlugin.machinesToCopy.Add(machine);
                Vector3 relativePosition = machine.GetGridInfo().BottomCenter - copyRegionAnchor;

                if (debugFunction) Debug.Log($"endCopying() relativePosition: {relativePosition}");

                blueprint.machineRelativePositions.Add(new MyVector3(relativePosition).ToString());
            }
        }

        // Private Functions

        private static void InitialiseCopying() {
            copyRegionMinExpansion = new Vector3();
            copyRegionMaxExpansion = new Vector3();
            copyRegionBounds = new Bounds(copyRegionStart, Vector3.zero);
            isCopying = true;
        }

        private static void GetAndShowDisplayBox() {
            if(copyRegionDisplayBox == null) {
                FieldInfo takeResourcesBoxInfo = Player.instance.interaction.GetType().GetField("takeResourcesBox", BindingFlags.Instance | BindingFlags.NonPublic);
                GameObject takeResourcesBox = (GameObject)takeResourcesBoxInfo.GetValue(Player.instance.interaction);
                copyRegionDisplayBox = GameObject.Instantiate(takeResourcesBox);
            }
            
            copyRegionDisplayBox.SetActive(true);
            UpdateDisplayBox();
        }

        private static void CancelCopying(bool notify) {
            isCopying = false;
            copyRegionDisplayBox.SetActive(false);
            if(notify) BlueprintsPlugin.Notify("Canceled copying");
        }

        private static void UpdateExpansion() {
            Vector3 camFacing = Player.instance.cam.transform.forward;
            Vector3 expansionDirection = Vector3.zero;

            Vector3 left = -Vector3.Cross(Vector3.up, camFacing).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, camFacing).normalized;
            Vector3 forward = camFacing;
            Vector3 backward = -camFacing;
            forward.y = 0;
            backward.y = 0;
            bool shrink = false;

            // Grow
            if (BlueprintsPlugin.nudgeLeftShortcut.Value.IsDown()) expansionDirection = AimingHelper.clampToAxis(left);
            else if (BlueprintsPlugin.nudgeRightShortcut.Value.IsDown()) expansionDirection = AimingHelper.clampToAxis(right);
            else if (BlueprintsPlugin.nudgeForwardShortcut.Value.IsDown()) expansionDirection = AimingHelper.clampToAxis(forward.normalized);
            else if(BlueprintsPlugin.nudgeBackwardShortcut.Value.IsDown()) expansionDirection = AimingHelper.clampToAxis(backward.normalized);
            else if (BlueprintsPlugin.nudgeUpShortcut.Value.IsDown()) expansionDirection = Vector3.up;
            else if (BlueprintsPlugin.nudgeDownShortcut.Value.IsDown()) expansionDirection = Vector3.down;

            // Shrink
            else if (BlueprintsPlugin.shrinkLeftShortcut.Value.IsDown()) {
                expansionDirection = AimingHelper.clampToAxis(left);
                shrink = true;
            }
            else if (BlueprintsPlugin.shrinkRightShortcut.Value.IsDown()) {
                expansionDirection = AimingHelper.clampToAxis(right);
                shrink = true;
            }
            else if (BlueprintsPlugin.shrinkForwardShortcut.Value.IsDown()) {
                expansionDirection = AimingHelper.clampToAxis(forward);
                shrink = true;
            }
            else if (BlueprintsPlugin.shrinkBackwardShortcut.Value.IsDown()) {
                expansionDirection = AimingHelper.clampToAxis(backward);
                shrink = true;
            }
            else if (BlueprintsPlugin.shrinkUpShortcut.Value.IsDown()) {
                expansionDirection = AimingHelper.clampToAxis(Vector3.up);
                shrink = true;
            }
            else if (BlueprintsPlugin.shrinkDownShortcut.Value.IsDown()) {
                expansionDirection = AimingHelper.clampToAxis(Vector3.down);
                shrink = true;
            }

            float growOrShrink = shrink ? -1 : 1;
            if (expansionDirection.x < 0 || expansionDirection.y < 0 || expansionDirection.z < 0) {
                copyRegionMinExpansion += growOrShrink * expansionDirection;
            }
            else if (expansionDirection.x > 0 || expansionDirection.y > 0 || expansionDirection.z > 0) {
                copyRegionMaxExpansion += growOrShrink * expansionDirection;
            }

            if (copyRegionMinExpansion.x > 0) copyRegionMinExpansion.x = 0;
            if (copyRegionMinExpansion.y > 0) copyRegionMinExpansion.y = 0;
            if (copyRegionMinExpansion.z > 0) copyRegionMinExpansion.z = 0;

            if (copyRegionMaxExpansion.x < 0) copyRegionMaxExpansion.x = 0;
            if (copyRegionMaxExpansion.y < 0) copyRegionMaxExpansion.y = 0;
            if (copyRegionMaxExpansion.z < 0) copyRegionMaxExpansion.z = 0;

            copyRegionBounds.SetMinMax(copyRegionBounds.min + copyRegionMinExpansion, copyRegionBounds.max + copyRegionMaxExpansion);
        }

        private static void UpdateDisplayBox() {
            copyRegionDisplayBox.transform.localScale = copyRegionBounds.size;
            copyRegionDisplayBox.transform.position = copyRegionBounds.min;
            copyRegionDisplayBox.transform.rotation = Quaternion.identity;
        }

        private static Vector3 GetFinalCopyRegionValues() {
            Vector3 size = copyRegionEnd - copyRegionStart;
            copyRegionBounds.Encapsulate(copyRegionStart + size);

            size.x = size.x >= 0 ? copyRegionBounds.size.x : -copyRegionBounds.size.x;
            size.y = copyRegionBounds.size.y;
            size.z = size.z >= 0 ? copyRegionBounds.size.z : -copyRegionBounds.size.z;

            copyRegionAnchor = new Vector3() {
                x = copyRegionBounds.center.x,
                y = copyRegionBounds.center.y - copyRegionBounds.extents.y,
                z = copyRegionBounds.center.z
            };

            return size;
        }

        private static HashSet<GenericMachineInstanceRef> GetMachineRefsInBounds() {
            bool debugFunction = false;
            copyRegionBounds.extents -= new Vector3(0.01f, 0.01f, 0.01f);

            if (debugFunction) {
                Debug.Log($"getMachineRefsInBounds() copyRegionBounds.center: {copyRegionBounds.center}");
                Debug.Log($"getMachineRefsInBounds() copyRegionBounds.extents: {copyRegionBounds.extents}");
                Debug.Log($"getMachineRefsInBounds() copyRegionBounds.min: {copyRegionBounds.min}");
                Debug.Log($"getMachineRefsInBounds() copyRegionBounds.max: {copyRegionBounds.max}");
            }

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

        private static HashSet<IMachineInstanceRef> GetMachinesToCopy() {
            HashSet<GenericMachineInstanceRef> machineRefs = GetMachineRefsInBounds();
            HashSet<IMachineInstanceRef> machines = new HashSet<IMachineInstanceRef>();
            foreach (GenericMachineInstanceRef machineRef in machineRefs) {
                if (machineRef.IsValid()) {
                    if (machineRef.typeIndex == MachineTypeEnum.TransitCable) {
                        Debug.Log($"Skipping monorail track");
                        continue;
                    }

                    if(machineRef.typeIndex == MachineTypeEnum.Accumulator) {
                        machines.Add(new MachineInstanceRef<AccumulatorInstance>(machineRef));
                    }
                    else {
                        machines.Add(machineRef.CastToInterface());
                    }
                }
            }

            return machines;
        }
    }
}
