using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        public MyVector3 size;
        public MyVector3 anchorPoint;
        public int rotation = 0;
        public List<uint> machineIDs = new List<uint>();
        public List<int> machineResIDs = new List<int>();
        public List<int> machineTypes = new List<int>();
        public List<int> machineRotations = new List<int>();
        public List<int> machineRecipes = new List<int>();
        public List<int> conveyorShapes = new List<int>();
        public List<bool> conveyorBuildBackwards = new List<bool>();
        public List<int> chestSizes = new List<int>();
        
        public List<MyVector3> machineRelativePositions = new List<MyVector3>();
        private Dictionary<uint, int> newMachineRotations = new Dictionary<uint, int>();

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
            Debug.Log("Rotating CW");
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
            Debug.Log("Rotating CCW");
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
            size = new MyVector3(newSize);
            ninetyDegSize = new MyVector3() {
                x =  size.z,
                y =  size.y,
                z = -size.x
            };
            oneEightyDegSize = new MyVector3() {
                x = -size.x,
                y = size.y,
                z = -size.z
            };
            twoSeventyDegSize = new MyVector3() {
                x = -size.z,
                y =  size.y,
                z =  size.x
            };
        }

        public Vector3 getRotatedSize() {
            switch (rotation) {
                default:
                case 0: return size.asUnityVector3();
                case 1: return ninetyDegSize.asUnityVector3();
                case 2: return oneEightyDegSize.asUnityVector3();
                case 3: return twoSeventyDegSize.asUnityVector3();
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
            List<Vector3> machineRelativePositionsAsUnityVectors = new List<Vector3>();
            foreach(MyVector3 vector in machineRelativePositions) {
                machineRelativePositionsAsUnityVectors.Add(vector.asUnityVector3());
            }

            switch (rotation) {
                case 0: rotatedPostions = machineRelativePositionsAsUnityVectors; break;
                case 1: 
                    foreach(Vector3 position in machineRelativePositionsAsUnityVectors) {
                        rotatedPostions.Add(new Vector3() {
                            x = position.z,
                            y = position.y,
                            z = -position.x
                        });
                    }
                    break;
                case 2:
                    foreach(Vector3 position in machineRelativePositionsAsUnityVectors) {
                        rotatedPostions.Add(new Vector3() {
                            x = -position.x,
                            y = position.y,
                            z = -position.z
                        });
                    }
                    break;
                case 3:
                    foreach(Vector3 position in machineRelativePositionsAsUnityVectors) {
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

    public class MyVector3 {
        public float x, y, z;

        public MyVector3(){}
        public MyVector3(Vector3 unityVector) {
            x = unityVector.x;
            y = unityVector.y;
            z = unityVector.z;
        }

        public Vector3 asUnityVector3() {
            return new Vector3(x, y, z);
        }
    }
}
