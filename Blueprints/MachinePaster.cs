using EquinoxsModUtils;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator.Modules;
using Mirror;
using ProceduralNoiseProject;
using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static ConveyorBuildInfo;

namespace Blueprints
{
    public static class MachinePaster
    {
        // Objects & Variables
        public static bool isPasting = false;
        public static bool isPositionLocked = false;
        public static List<Vector3> rotatedRelativePositions;
        public static Vector3 finalAimLocation;

        private static Blueprint clipboard => BlueprintsPlugin.clipboard;
        private static List<StreamedHologramData> holograms = new List<StreamedHologramData>();
        private static Vector3 nudgeOffset;
        public static Vector3 lockedPosition;

        // Public Functions

        public static void StartPasting() {
            bool debugFunction = false;

            if (clipboard == null) {
                BlueprintsPlugin.Notify("Nothing to paste!");
                return;
            }

            rotatedRelativePositions = clipboard.GetMachineRelativePositions();
            isPasting = true;
            nudgeOffset = Vector3.zero;
            finalAimLocation = AimingHelper.getAimedLocationForPasting();
            lockedPosition = finalAimLocation;

            renderHolograms();
        }

        public static void updateHolograms() {
            if (BlueprintsPlugin.cancelShortcut.Value.IsDown()) {
                hideHolograms();
                isPasting = false;
                isPositionLocked = false;
                BlueprintsPlugin.Notify("Canceled pasting");
            }

            Vector3 currentAim = AimingHelper.getAimedLocationForPasting();
            if (!isPositionLocked) lockedPosition = currentAim;

            Vector3Int blueprintSize = clipboard.GetRotatedSize();
            if (blueprintSize.x % 2 == 0) currentAim.x += 0.5f;
            if (blueprintSize.z % 2 == 0) currentAim.z += 0.5f;

            checkForNudging();
            currentAim += nudgeOffset;

            for (int i = 0; i < holograms.Count; i++) {
                Vector3 newLocation = currentAim + rotatedRelativePositions[i];

                int newYaw = clipboard.machineRotations[i] + clipboard.GetMachineRotation(clipboard.machineIDs[i]);
                newYaw = newYaw % 360;
                if (newYaw < 0) newYaw += 360;

                holograms[i].SetTransform(newLocation, Quaternion.Euler(0, newYaw, 0));

                if (holograms[i] is ConveyorHologramData conveyorHologram) {
                    conveyorHologram.topYawRot = clipboard.conveyorTopYawRots[i] + clipboard.rotation * 90;
                }
            }

            if (BlueprintsPlugin.cwRotateShortcut.Value.IsDown() && !BlueprintsLibraryGUI.shouldShow) {
                clipboard.RotateCW();
                rotatedRelativePositions = clipboard.GetMachineRelativePositions();
            }
            else if (BlueprintsPlugin.ccwRotateShortcut.Value.IsDown() && !BlueprintsLibraryGUI.shouldShow) {
                clipboard.RotateCCW();
                rotatedRelativePositions = clipboard.GetMachineRelativePositions();
            }
        }

        public static void endPasting() {
            isPasting = false;
            finalAimLocation = AimingHelper.getAimedLocationForPasting();

            for(int i  = 0; i < clipboard.machineIDs.Count; i++) {
                BuildQueue.holograms.Add(holograms[i]);
                BuildQueue.queuedBuildings.Add(new QueuedBuilding() {
                    machineID = clipboard.machineIDs[i],
                    index = clipboard.machineIndexes[i],
                    resID = clipboard.machineResIDs[i],
                    type = clipboard.machineTypes[i],
                    rotation = clipboard.machineTypes[i],
                    recipe = clipboard.machineRecipes[i],
                    variationIndex = clipboard.machineVariationIndexes[i],
                    dimensions = new MyVector3(clipboard.machineDimensions[i]),
                    conveyorShape = clipboard.conveyorShapes[i],
                    conveyorBuildBackwards = clipboard.conveyorBuildBackwards[i],
                    conveyorHeight = clipboard.conveyorHeights[i],
                    conveyorInputBottom = clipboard.conveyorInputBottoms[i],
                    conveyorTopYawRot = clipboard.conveyorTopYawRots[i] + clipboard.rotation * 90,
                    chestSize = clipboard.chestSizes[i],
                    gridInfo = getNewGridInfo(rotatedRelativePositions, i, clipboard.machineRotations[i], clipboard.machineIDs[i], clipboard.machineVariationIndexes[i])
                });
            }

            holograms.Clear();
            postPaste();
        }

        public static GridInfo getNewGridInfo(List<Vector3> rotatedRelativePositions, int index, int yawRotation, uint id, int variationIndex) {
            bool debugFunction = false;

            GridInfo newGridInfo = new GridInfo();

            Vector3 aimLocation = finalAimLocation;
            if (debugFunction) Debug.Log($"getNewGridInfo() aimLocation before nudge: {aimLocation}");

            if(debugFunction && isPositionLocked) {
                Debug.Log($"getNewGridInfo() lockedPosition: {lockedPosition}");
            }

            aimLocation += nudgeOffset;
            if (debugFunction) Debug.Log($"getNewGridInfo() nudgeOffset: {nudgeOffset}");
            if (debugFunction) Debug.Log($"getNewGridInfo() aimLocation after nudge: {aimLocation}");

            Vector3Int blueprintSize = clipboard.GetRotatedSize();
            if (debugFunction) Debug.Log($"getNewGridInfo() blueprintSize: {blueprintSize}");

            if (blueprintSize.x % 2 == 0) aimLocation.x += 0.5f;
            if (blueprintSize.z % 2 == 0) aimLocation.z += 0.5f;
            if (debugFunction) Debug.Log($"getNewGridInfo() aimLocation after size offset: {aimLocation}");

            Vector3 dimensions = clipboard.GetMachineDimensions(index);
            Vector3 newMachineCenter = aimLocation + rotatedRelativePositions[index];
            Vector3 newMinPos = new Vector3() {
                x = newMachineCenter.x - dimensions.x / 2.0f,
                y = newMachineCenter.y,
                z = newMachineCenter.z - dimensions.z / 2.0f
            };
            Vector3Int newMinPosInts = new Vector3Int() {
                x = Mathf.RoundToInt(newMinPos.x),
                y = Mathf.RoundToInt(newMinPos.y),
                z = Mathf.RoundToInt(newMinPos.z),
            };
            newGridInfo.minPos = newMinPosInts;

            if (debugFunction) {
                Debug.Log($"getNewGridInfo() machine dimensions: {dimensions}");
                Debug.Log($"getNewGridInfo() newMachineenter: {newMachineCenter}");
                Debug.Log($"getNewGridInfo() newMinPos: {newMinPos}");
                Debug.Log($"getNewGridInfo() newMinPosInts: {newMinPosInts}");
            }

            int newYaw = yawRotation + clipboard.GetMachineRotation(id);
            if (newYaw >= 360) newYaw -= 360;
            else if (newYaw < 0) newYaw += 360;
            newGridInfo.SetYawRotation(newYaw);

            newGridInfo.SetVariationIndex(variationIndex);

            return newGridInfo;
        }

        public static void postPaste() {
            isPositionLocked = false;
            clipboard.rotation = 0;
            clipboard.ClearMachineRotations();
        }

        public static void hideHolograms() {
            foreach (StreamedHologramData hologram in holograms) {
                hologram.AbandonHologramPreview();
            }

            holograms.Clear();
        }

        // Private Functions

        private static void renderHolograms() {
            bool debugFunction = false;

            Vector3 aimLocation = AimingHelper.getAimedLocationForPasting();
            lockedPosition = aimLocation;

            if (debugFunction) Debug.Log($"renderHolograms() aimLocation: {aimLocation}");

            Vector3Int blueprintSize = clipboard.GetRotatedSize();
            if (blueprintSize.x % 2 == 0) aimLocation.x += 0.5f;
            if (blueprintSize.z % 2 == 0) aimLocation.z += 0.5f;

            if (debugFunction) {
                Debug.Log($"renderHolograms() blueprintSize: {blueprintSize}");
                Debug.Log($"renderHolograms() aimLocation after size offset: {aimLocation}");
            }

            for (int i = 0; i < clipboard.machineIDs.Count; i++) {
                int resID = clipboard.machineResIDs[i];
                int machineIndex = clipboard.machineIndexes[i];
                MachineTypeEnum type = (MachineTypeEnum)clipboard.machineTypes[i];
                Vector3 offset = rotatedRelativePositions[i];
                float yawRotation = clipboard.machineRotations[i];
                int variationIndex = clipboard.machineVariationIndexes[i];

                BuilderInfo builderInfo = SaveState.GetResInfoFromId(resID) as BuilderInfo;

                if (debugFunction) {
                    Debug.Log($"renderHolograms() machineIndex: {machineIndex}");
                    Debug.Log($"renderHolograms() machineType: {type}");
                    Debug.Log($"renderHolograms() offset: {offset}");
                }

                Vector3 thisHologramPos = aimLocation + offset;
                if (debugFunction) Debug.Log($"renderHolograms() thisHologramPos before dim offset: {thisHologramPos}");

                if (type == MachineTypeEnum.Conveyor) {
                    if (debugFunction) Debug.Log("renderingHolograms() rendering conveyor");

                    ConveyorHologramData conveyorHologram = builderInfo.GenerateUnbuiltHologramData() as ConveyorHologramData;
                    conveyorHologram.buildBackwards = clipboard.conveyorBuildBackwards[i];
                    conveyorHologram.curShape = (ConveyorInstance.BeltShape)clipboard.conveyorShapes[i];
                    conveyorHologram.numBelts = 1;
                    if(conveyorHologram.curShape == ConveyorInstance.BeltShape.Vertical) {
                        conveyorHologram.numBelts = clipboard.conveyorHeights[i];
                        conveyorHologram.verticalHeight = clipboard.conveyorHeights[i];
                        conveyorHologram.inputBottom = clipboard.conveyorInputBottoms[i];
                        conveyorHologram.topYawRot = clipboard.conveyorTopYawRots[i] + (clipboard.rotation * 90);
                    }

                    yawRotation = clipboard.machineRotations[i];

                    Quaternion conveyorRotation = Quaternion.Euler(0, yawRotation, 0);
                    conveyorHologram.SetTransform(thisHologramPos, conveyorRotation);
                    conveyorHologram.ShowUnbuilt(true, true);
                    holograms.Add(conveyorHologram);

                    if (debugFunction) Debug.Log("renderHolograms() added conveyor to holograms");

                    continue;
                }

                if (debugFunction) Debug.Log("renderingHolograms() rendering non-conveyor");

                StreamedHologramData hologram = builderInfo.GenerateUnbuiltHologramData();

                if(variationIndex != -1) hologram.variationNum = variationIndex;

                Quaternion rotation = Quaternion.Euler(0, yawRotation, 0);
                hologram.SetTransform(thisHologramPos, rotation);
                hologram.ShowUnbuilt(true, true);
                holograms.Add(hologram);
            }
        }

        private static void checkForNudging() {
            Vector3 camFacing = Player.instance.cam.transform.forward;
            Vector3 nudgeDirection = Vector3.zero;

            Vector3 left = -Vector3.Cross(Vector3.up, camFacing).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, camFacing).normalized;
            Vector3 forward = camFacing;
            Vector3 backward = -camFacing;
            forward.y = 0;
            backward.y = 0;

            if (BlueprintsPlugin.nudgeLeftShortcut.Value.IsDown()) nudgeDirection = AimingHelper.clampToAxis(left);
            else if (BlueprintsPlugin.nudgeRightShortcut.Value.IsDown()) nudgeDirection = AimingHelper.clampToAxis(right);
            else if (BlueprintsPlugin.nudgeForwardShortcut.Value.IsDown()) nudgeDirection = AimingHelper.clampToAxis(forward);
            else if (BlueprintsPlugin.nudgeBackwardShortcut.Value.IsDown()) nudgeDirection = AimingHelper.clampToAxis(backward);
            else if (BlueprintsPlugin.nudgeUpShortcut.Value.IsDown()) nudgeDirection = Vector3.up;
            else if (BlueprintsPlugin.nudgeDownShortcut.Value.IsDown()) nudgeDirection = Vector3.down;

            if(nudgeDirection != Vector3.zero) {
                nudgeOffset += nudgeDirection;
            }
        }
    }
}
