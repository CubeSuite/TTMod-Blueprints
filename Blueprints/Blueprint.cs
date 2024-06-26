﻿using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Events;
using UnityEngine.Search;
using static Voxeland5.CustomSerialization;

namespace Blueprints
{
    [Serializable]
    public class Blueprint
    {
        public int id = -1;
        public int parentId;
        public string name = "New Blueprint";
        public string icon;
        public string description;
        public string size;
        public int rotation = 0;
        public List<uint> machineIDs = new List<uint>();
        public List<int> machineIndexes = new List<int>();
        public List<int> machineResIDs = new List<int>();
        public List<int> machineTypes = new List<int>();
        public List<int> machineRotations = new List<int>();
        public List<int> machineRecipes = new List<int>();
        public List<int> machineVariationIndexes = new List<int>();
        public List<string> machineDimensions = new List<string>();
        public List<int> conveyorShapes = new List<int>();
        public List<bool> conveyorBuildBackwards = new List<bool>();
        public List<int> conveyorHeights = new List<int>();
        public List<bool> conveyorInputBottoms = new List<bool>();
        public List<int> conveyorTopYawRots = new List<int>();
        public List<int> chestSizes = new List<int>();
        
        public List<string> machineRelativePositions = new List<string>();
        private Dictionary<uint, int> newMachineRotations = new Dictionary<uint, int>();

        private MyVector3 ninetyDegSize;
        private MyVector3 oneEightyDegSize;
        private MyVector3 twoSeventyDegSize;

        // Public Functions

        public string ToJson(bool formatted = false) {
            return JsonUtility.ToJson(this, formatted);
        }

        public List<MachineCost> GetCost() {
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
                bool affordable = Player.instance.inventory.HasResources(pair.Key, pair.Value);
                cost.Add(new MachineCost(pair.Key, pair.Value, affordable));
            }

            return cost;
        }

        public bool CanAfford(bool notify = false) {
            List<MachineCost> missing = GetCost().Where(machine => !machine.affordable).ToList();
            if (missing.Count == 0) return true;

            foreach (MachineCost cost in missing) {
                ResourceInfo info = SaveState.GetResInfoFromId(cost.resId);
                int have = Player.instance.inventory.GetResourceCount(cost.resId);
                BlueprintsPlugin.Notify($"Missing {info.displayName} {have} / {cost.count}");
            }

            return false;
        }

        public void RotateCW() {
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

        public void RotateCCW() {
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

        public MyVector3 GetSize() {
            return new MyVector3(size);
        }

        public void SetSize(Vector3 newSize) {
            size = $"{Mathf.RoundToInt(newSize.x)},{Mathf.RoundToInt(newSize.y)},{Mathf.RoundToInt(newSize.z)}";

            ninetyDegSize = new MyVector3() {
                x = Mathf.RoundToInt( newSize.z),
                y = Mathf.RoundToInt( newSize.y),
                z = Mathf.RoundToInt(-newSize.x)
            };
            oneEightyDegSize = new MyVector3() {
                x = Mathf.RoundToInt(-newSize.x),
                y = Mathf.RoundToInt( newSize.y),
                z = Mathf.RoundToInt(-newSize.z)
            };
            twoSeventyDegSize = new MyVector3() {
                x = Mathf.RoundToInt(-newSize.z),
                y = Mathf.RoundToInt( newSize.y),
                z = Mathf.RoundToInt( newSize.x)
            };
        }

        public Vector3Int GetRotatedSize() {
            switch (rotation) {
                default:
                case 0: return GetSize().asUnityVector3Int();
                case 1: return ninetyDegSize.asUnityVector3Int();
                case 2: return oneEightyDegSize.asUnityVector3Int();
                case 3: return twoSeventyDegSize.asUnityVector3Int();
            }
        }

        public int GetMachineRotation(uint instanceId) {
            if (newMachineRotations.ContainsKey(instanceId)) {
                return newMachineRotations[instanceId];
            }
            else {
                return 0;
            }
        }

        public Vector3 GetMachineDimensions(int index) {


            switch (rotation) {
                case 0:
                case 2:
                    return new MyVector3(machineDimensions[index]).AsUnityVector3();

                case 1:
                case 3:
                    MyVector3 dimensions = new MyVector3(machineDimensions[index]);
                    return new Vector3() {
                        x = dimensions.z,
                        y = dimensions.y,
                        z = dimensions.x
                    };
            }

            return Vector3.zero;
        }

        public void ClearMachineRotations() {
            newMachineRotations.Clear();
        }

        public List<Vector3> GetMachineRelativePositions() {
            List<Vector3> rotatedPositions = new List<Vector3>();
            List<Vector3> machineRelativePositionsAsUnityVectors = new List<Vector3>();

            foreach(string vectorString in machineRelativePositions) {
                machineRelativePositionsAsUnityVectors.Add(new MyVector3(vectorString).AsUnityVector3());
            }

            // FHG if you read this you have to hire me
            // This is a legally binding comment

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

        public static Blueprint CreateFromClipboard() {
            Blueprint clone = new Blueprint() {
                id = -1,
                parentId = BookManager.currentBookId,
                name = "New Blueprint",
                icon = "",
                description = "",
                rotation = 0,
                machineIDs = BlueprintsPlugin.clipboard.machineIDs,
                machineIndexes = BlueprintsPlugin.clipboard.machineIndexes,
                machineResIDs = BlueprintsPlugin.clipboard.machineResIDs,
                machineTypes = BlueprintsPlugin.clipboard.machineTypes,
                machineRotations = BlueprintsPlugin.clipboard.machineRotations,
                machineRecipes = BlueprintsPlugin.clipboard.machineRecipes,
                machineVariationIndexes = BlueprintsPlugin.clipboard.machineVariationIndexes,
                machineDimensions = BlueprintsPlugin.clipboard.machineDimensions,
                conveyorShapes = BlueprintsPlugin.clipboard.conveyorShapes,
                conveyorBuildBackwards = BlueprintsPlugin.clipboard.conveyorBuildBackwards,
                conveyorHeights = BlueprintsPlugin.clipboard.conveyorHeights,
                conveyorInputBottoms = BlueprintsPlugin.clipboard.conveyorInputBottoms,
                conveyorTopYawRots = BlueprintsPlugin.clipboard.conveyorTopYawRots,
                chestSizes = BlueprintsPlugin.clipboard.chestSizes,
                machineRelativePositions = BlueprintsPlugin.clipboard.machineRelativePositions
            };

            clone.SetSize(BlueprintsPlugin.clipboard.GetSize().AsUnityVector3());
            return clone;
        }
    }

    [Serializable]
    public struct MachineCost {
        public int resId;
        public int count;
        public bool affordable;

        public MachineCost(int _resId, int _count, bool _affordable) {
            resId = _resId;
            count = _count;
            affordable = _affordable;
        }
    }

    [Serializable]
    public class MyVector3 {
        public float x;
        public float y;
        public float z;

        // Constructors

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

        public MyVector3(string input) {
            string[] parts = input.Split(',');
            x = float.Parse(parts[0]);
            y = float.Parse(parts[1]);
            z = float.Parse(parts[2]);
        }

        // Public Functions

        public override string ToString() {
            return $"{x},{y},{z}";
        }

        public Vector3 AsUnityVector3() {
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
