using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static Voxeland5.CustomSerialization;

namespace Blueprints
{
    public class Blueprint
    {
        public int id;
        public string name;
        public MyVector3 size;
        public MyVector3 anchorPoint;
        public int rotation = 0;
        public List<uint> machineIDs = new List<uint>();
        public List<int> machineIndexes = new List<int>();
        public List<int> machineResIDs = new List<int>();
        public List<int> machineTypes = new List<int>();
        public List<int> machineRotations = new List<int>();
        public List<int> machineRecipes = new List<int>();
        public List<int> machineVariationIndexes = new List<int>();
        public List<MyVector3> machineDimensions = new List<MyVector3>();
        public List<int> conveyorShapes = new List<int>();
        public List<bool> conveyorBuildBackwards = new List<bool>();
        public List<int> conveyorHeights = new List<int>();
        public List<bool> conveyorInputBottoms = new List<bool>();
        public List<int> conveyorTopYawRots = new List<int>();
        public List<int> chestSizes = new List<int>();
        
        public List<MyVector3> machineRelativePositions = new List<MyVector3>();
        private Dictionary<uint, int> newMachineRotations = new Dictionary<uint, int>();

        private int numMachines => machineIDs.Count;

        private MyVector3 ninetyDegSize;
        private MyVector3 oneEightyDegSize;
        private MyVector3 twoSeventyDegSize;

        // Public Functions

        public string toJson(bool formatted = false) {
            return JsonConvert.SerializeObject(this);
        }

        public List<MachineCost> getCost() {
            List<MachineCost> cost = new List<MachineCost>();
            Dictionary<int, int> machineCounts = new Dictionary<int, int>();

            foreach(int resId in machineResIDs) {
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
            ++rotation;
            if (rotation == 4) rotation = 0;

            foreach(uint id in machineIDs) {
                if (!newMachineRotations.ContainsKey(id)) {
                    newMachineRotations.Add(id, 90);
                }
                else {
                    newMachineRotations[id] += 90;
                }
            }
        }

        public void rotateCCW() {
            --rotation;
            if (rotation == -1) rotation = 3;

            foreach(uint id in machineIDs) {
                if (!newMachineRotations.ContainsKey(id)) {
                    newMachineRotations.Add(id, -90);
                }
                else {
                    newMachineRotations[id] -= 90;
                }
            }
        }

        public void setSize(Vector3 newSize) {
            size = new MyVector3(newSize) {
                x = Mathf.RoundToInt(newSize.x),
                y = Mathf.RoundToInt(newSize.y),
                z = Mathf.RoundToInt(newSize.z),
            };
            ninetyDegSize = new MyVector3() {
                x = Mathf.RoundToInt( size.z),
                y = Mathf.RoundToInt( size.y),
                z = Mathf.RoundToInt(-size.x)
            };
            oneEightyDegSize = new MyVector3() {
                x = Mathf.RoundToInt(-size.x),
                y = Mathf.RoundToInt( size.y),
                z = Mathf.RoundToInt(-size.z)
            };
            twoSeventyDegSize = new MyVector3() {
                x = Mathf.RoundToInt(-size.z),
                y = Mathf.RoundToInt( size.y),
                z = Mathf.RoundToInt( size.x)
            };
        }

        public Vector3Int getRotatedSize() {
            switch (rotation) {
                default:
                case 0: return size.asUnityVector3Int();
                case 1: return ninetyDegSize.asUnityVector3Int();
                case 2: return oneEightyDegSize.asUnityVector3Int();
                case 3: return twoSeventyDegSize.asUnityVector3Int();
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

        public Vector3 getMachineDimensions(int index) {
            switch (rotation) {
                case 0:
                case 2:
                    return machineDimensions[index].asUnityVector3();

                case 1:
                case 3:
                    return new Vector3() {
                        x = machineDimensions[index].z,
                        y = machineDimensions[index].y,
                        z = machineDimensions[index].x
                    };
            }

            return Vector3.zero;
        }

        public void clearMachineRotations() {
            newMachineRotations.Clear();
        }

        public List<Vector3> getMachineRelativePositions() {
            List<Vector3> rotatedPositions = new List<Vector3>();
            List<Vector3> machineRelativePositionsAsUnityVectors = new List<Vector3>();

            foreach(MyVector3 vector in machineRelativePositions) {
                machineRelativePositionsAsUnityVectors.Add(vector.asUnityVector3());
            }

            switch (rotation) {
                case 0: rotatedPositions = machineRelativePositionsAsUnityVectors; break;
                case 1: 
                    foreach(Vector3 position in machineRelativePositionsAsUnityVectors) {
                        rotatedPositions.Add(new Vector3() {
                            x = position.z,
                            y = position.y,
                            z = -position.x
                        });
                    }
                    break;
                case 2:
                    foreach(Vector3 position in machineRelativePositionsAsUnityVectors) {
                        rotatedPositions.Add(new Vector3() {
                            x = -position.x,
                            y = position.y,
                            z = -position.z
                        });
                    }
                    break;
                case 3:
                    foreach(Vector3 position in machineRelativePositionsAsUnityVectors) {
                        rotatedPositions.Add(new Vector3() {
                            x = -position.z,
                            y = position.y,
                            z = position.x
                        });
                    }
                    break;
            }

            return rotatedPositions;
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

    public class MyVector3 {
        public float x, y, z;

        public MyVector3(){}
        public MyVector3(Vector3 unityVector) {
            x = unityVector.x;
            y = unityVector.y;
            z = unityVector.z;
        }

        public MyVector3(Vector3Int unityVector) {
            x = unityVector.x;
            y = unityVector.y;
            z = unityVector.z;
        }

        public override string ToString() {
            return $"({x},{y},{z})";
        }

        public Vector3 asUnityVector3() {
            return new Vector3(x, y, z);
        }

        public Vector3Int asUnityVector3Int() {
            return new Vector3Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Mathf.RoundToInt(z));
        }

        public static MyVector3 operator /(MyVector3 vector, float f) {
            return new MyVector3() {
                x = vector.x / f,
                y = vector.y / f,
                z = vector.z / f
            };
        }
    }
}
