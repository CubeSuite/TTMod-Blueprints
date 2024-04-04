using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints
{
    public static class BuildQueue
    {
        // Objects & Variables

        public static bool shouldShowBuildQueue => queuedBuildings.Count != 0;
        public static List<QueuedBuilding> queuedBuildings = new List<QueuedBuilding>();

        // Public Functions

        public static void ShowBuildQueue() {
            float windowWidth = 300;
            float windowHeight = 600;
            float x = Screen.width - windowWidth;
            float y = (Screen.height - windowHeight) / 2.0f;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.window) {
                fontSize = 16,
                alignment = TextAnchor.UpperCenter,

            };

            GUI.backgroundColor = new Color32(22, 22, 38, 192);
            GUI.Window(0, new Rect(x, y, windowWidth, windowHeight), null, "Build Queue", titleStyle);

            int maxNameLength = GetItemNamePadding();
            string title = "Item".PadRight(maxNameLength) + " Have / Need\n";
            StringBuilder queueLabel = new StringBuilder(title);

            List<int> doneResIDs = new List<int>();
            foreach (QueuedBuilding building in queuedBuildings) {
                if (doneResIDs.Contains(building.resID)) continue;
                string have = Player.instance.inventory.GetResourceCount(building.resID).ToString().PadRight(4);
                string need = queuedBuildings.Where(machine => machine.resID == building.resID).Count().ToString().PadRight(4);
                queueLabel.AppendLine($"{SaveState.GetResInfoFromId(building.resID).displayName.PadRight(maxNameLength)} {have} / {need}");
                doneResIDs.Add(building.resID);
            }

            GUI.Box(new Rect(x, y + 20, windowWidth, windowHeight), queueLabel.ToString());
        }

        // Private Functions

        private static int GetItemNamePadding() {
            int max = 0;
            foreach(QueuedBuilding building in queuedBuildings) {
                int length = SaveState.GetResInfoFromId(building.resID).displayName.Length;
                if (length > max) max = length;
            }

            return Math.Max(max, 50);
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
        public Vector3 relativePosition;
    }
}
