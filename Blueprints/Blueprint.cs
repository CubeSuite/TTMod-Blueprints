using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static Voxeland5.CustomSerialization;

namespace Blueprints
{
    public class Blueprint
    {
        public int id;
        public string name;
        public Vector3 size;
        public int rotation = 0;
        public List<IMachineInstanceRef> machines = new List<IMachineInstanceRef>();
        public List<GenericMachineInstanceRef> genericMachineInstanceRefs = new List<GenericMachineInstanceRef>();
        
        public List<Vector3> machineRelativePositions = new List<Vector3>();
        private Dictionary<uint, int> newMachineRotations = new Dictionary<uint, int>();

        private Vector3 ninetyDegSize;
        private Vector3 oneEightyDegSize;
        private Vector3 twoSeventyDegSize;

        // Public Functions

        public string toJson(bool formatted = false) {
            return JsonUtility.ToJson(this, formatted);
        }

        public List<MachineCost> getCost() {
            List<MachineCost> cost = new List<MachineCost>();
            Dictionary<int, int> machineCounts = new Dictionary<int, int>();

            foreach(IMachineInstanceRef machine in machines) {
                int resId = machine.GetCommonInfo().resId;
                if (machineCounts.ContainsKey(resId)){
                    ++machineCounts[resId];
                }
                else {
                    machineCounts[resId] = 1;
                }
            }

            foreach(KeyValuePair<int, int> pair in  machineCounts) {
                cost.Add(new MachineCost(pair.Key, pair.Value));
            }

            return cost;
        }

        public void rotateCW() {
            Debug.Log("Rotating CW");
            ++rotation;
            if (rotation == 4) rotation = 0;

            foreach(IMachineInstanceRef machine in machines) {
                if (!newMachineRotations.ContainsKey(machine.instanceId)) {
                    newMachineRotations.Add(machine.instanceId, 90);
                }
                else {
                    newMachineRotations[machine.instanceId] += 90;
                }
            }
        }

        public void rotateCCW() {
            Debug.Log("Rotating CCW");
            --rotation;
            if (rotation == -1) rotation = 3;

            foreach(IMachineInstanceRef machine in machines) {
                if (!newMachineRotations.ContainsKey(machine.instanceId)) {
                    newMachineRotations.Add(machine.instanceId, -90);
                }
                else {
                    newMachineRotations[machine.instanceId] -= 90;
                }
            }
        }

        public void setSize(Vector3 newSize) {
            size = newSize;
            ninetyDegSize = new Vector3() {
                x =  size.z,
                y =  size.y,
                z = -size.x
            };
            oneEightyDegSize = new Vector3() {
                x = -size.x,
                y = size.y,
                z = -size.z
            };
            twoSeventyDegSize = new Vector3() {
                x = -size.z,
                y =  size.y,
                z =  size.x
            };
        }

        public Vector3 getRotatedSize() {
            switch (rotation) {
                default:
                case 0: return size;
                case 1: return ninetyDegSize;
                case 2: return oneEightyDegSize;
                case 3: return twoSeventyDegSize;
            }
        }

        public int getMachineRotation(uint instanceId) {
            if (newMachineRotations.ContainsKey(instanceId)) {
                return newMachineRotations[instanceId];
            }
            else {
                return 0;
            }
        }

        public void clearMachineRotations() {
            newMachineRotations.Clear();
        }

        public List<Vector3> getMachineRelativePositions() {
            List<Vector3> rotatedPostions = new List<Vector3>();
            switch (rotation) {
                case 0: rotatedPostions = machineRelativePositions; break;
                case 1: 
                    foreach(Vector3 position in machineRelativePositions) {
                        rotatedPostions.Add(new Vector3() {
                            x = position.z,
                            y = position.y,
                            z = -position.x
                        });
                    }
                    break;
                case 2:
                    foreach(Vector3 position in machineRelativePositions) {
                        rotatedPostions.Add(new Vector3() {
                            x = -position.x,
                            y = position.y,
                            z = -position.z
                        });
                    }
                    break;
                case 3:
                    foreach(Vector3 position in machineRelativePositions) {
                        rotatedPostions.Add(new Vector3() {
                            x = -position.z,
                            y = position.y,
                            z = position.x
                        });
                    }
                    break;
            }

            return rotatedPostions;
        }
    }

    public struct MachineCost {
        public int resId;
        public int count;

        public MachineCost(int _resId, int _count) {
            resId = _resId;
            count = _count;
        }
    }
}
