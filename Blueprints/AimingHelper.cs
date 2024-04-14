using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints
{
    public static class AimingHelper
    {
        // Objects & Variables
        public static LayerMask? buildablesMask = null;

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
                if (machineRef.instanceId != GenericMachineInstanceRef.INVALID_REFERENCE.instanceId && machineRef.MyGridInfo.Center != null) {
                    Vector3 result = machineRef.MyGridInfo.BottomCenter;
                    result.y += machineRef.MyGridInfo.dims.y;
                    return result;
                }
            }

            return null;
        }

        public static Vector3 getAimedLocationForPasting() {
            bool shouldLog = false;

            if (MachinePaster.isPositionLocked) {
                if(shouldLog) Debug.Log($"getAimedLocationForPasting() - Returning lockedLocation {MachinePaster.lockedPosition}");
                return MachinePaster.lockedPosition;
            }

            Vector3 currentAim = Player.instance.builder.CurrentAimTarget;
            if (shouldLog) Debug.Log($"getAimedLocationForPasting() - currentAim: {currentAim}");

            if (currentAim.x >= 0) currentAim.x += 0.5f - (currentAim.x % Mathf.Floor(currentAim.x));
            else currentAim.x += (Mathf.Abs(currentAim.x) % Mathf.Floor(Mathf.Abs(currentAim.x))) - 0.5f;
            if (shouldLog) Debug.Log($"getAimedLocationForPasting() - currentAim After X Adjust: {currentAim}");

            currentAim.y = Mathf.Floor(currentAim.y);
            
            if (currentAim.z >= 0) currentAim.z += 0.5f - (currentAim.z % Mathf.Floor(currentAim.z));
            else currentAim.z += (Mathf.Abs(currentAim.z) % Mathf.Floor(Mathf.Abs(currentAim.z))) - 0.5f;
            if (shouldLog) Debug.Log($"getAimedLocationForPasting() - currentAim After Z Adjust: {currentAim}");

            return currentAim;
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

        public static Vector3 clampToAxis(Vector3 vector) {
            vector.x = Mathf.RoundToInt(vector.x);
            vector.y = Mathf.RoundToInt(vector.y);
            vector.z = Mathf.RoundToInt(vector.z);
            return vector;
        }
    }
}
