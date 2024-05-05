using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Blueprints
{
    public static class BuildQueue
    {
        // Objects & Variables

        public static bool shouldShowBuildQueue => queuedBuildings.Count != 0;
        
        public static List<QueuedBuilding> queuedBuildings = new List<QueuedBuilding>();
        public static List<StreamedHologramData> holograms = new List<StreamedHologramData>();

        // Public Functions

        public static void ShowBuildQueue() {
            float windowWidth = 300;
            float windowHeight = 600;
            float x = Screen.width - windowWidth;
            float y = (Screen.height - windowHeight) / 2.0f;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.window) {
                fontSize = 16,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor  = Color.yellow }
            };

            GUI.backgroundColor = new Color32(22, 22, 38, 255);
            GUI.Window(0, new Rect(x, y, windowWidth, windowHeight), null, "Build Queue", titleStyle);

            StringBuilder namesLabel = new StringBuilder("Item\n");
            StringBuilder numbersLabel = new StringBuilder("Have / Need\n");

            List<int> doneResIDs = new List<int>();
            foreach (QueuedBuilding building in queuedBuildings) {
                if (doneResIDs.Contains(building.resID)) continue;

                namesLabel.AppendLine(SaveState.GetResInfoFromId(building.resID).displayName);
                numbersLabel.AppendLine(GetItemHaveNeed(building.resID));

                doneResIDs.Add(building.resID);
            }

            GUIStyle namesStyle = new GUIStyle(GUI.skin.box) {
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.yellow, background = null }
            };

            GUIStyle numbersStyle = new GUIStyle(GUI.skin.box) {
                alignment = TextAnchor.UpperRight,
                normal = { textColor = Color.white, background = null },
            };

            GUI.Box(new Rect(x, y + 20, windowWidth, windowHeight - 20), namesLabel.ToString(), namesStyle);
            GUI.Box(new Rect(x + 220, y + 20, windowWidth - 220, windowHeight - 20), numbersLabel.ToString(), numbersStyle);
        }

        public static void HideHologram(int index) {
            holograms[index].AbandonHologramPreview();
            holograms.RemoveAt(index);
        }

        public static void ClearHolograms() {
            foreach(StreamedHologramData hologram in holograms) {
                hologram.AbandonHologramPreview();
            }

            holograms.Clear();
        }

        // Private Functions

        private static string GetItemHaveNeed(int resID) {
            return $"{Player.instance.inventory.GetResourceCount(resID)} / {queuedBuildings.Where(machine => machine.resID == resID).Count()}";
        }
    }

    public class QueuedBuilding
    {
        public uint machineID;
        public int index;
        public int resID;
        public int type;
        public int rotation;
        public int recipe;
        public int variationIndex;
        public MyVector3 dimensions;
        public int conveyorShape;
        public bool conveyorBuildBackwards;
        public int conveyorHeight;
        public bool conveyorInputBottom;
        public int conveyorTopYawRot;
        public int chestSize;
        public GridInfo gridInfo;
    }
}
