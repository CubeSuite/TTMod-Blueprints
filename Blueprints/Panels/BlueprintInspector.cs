using EquinoxsModUtils;
using Rewired.UI.ControlMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Rewired.Integration.UnityUI.RewiredPointerInputModule;

namespace Blueprints.Panels
{
    public static class BlueprintInspector
    {
        // Objects & Variables
        public static float timeSinceExportClicked = 0;
        public static int deleteClicksRemaining = 10;
        
        private static Blueprint blueprint;
        private static float windowX;
        private static float windowY;

        // Field Values
        public static string blueprintName;
        public static string blueprintNameLastUpdate;
        
        public static string blueprintIconName;
        public static string blueprintIconNameLastUpdate;
        
        public static string blueprintDescription;
        public static string blueprintDescriptionLastUpdate;

        private static Vector2 scrollPos = Vector2.zero;

        // Styles
        private static GUIStyle titleStyle;
        private static GUIStyle headerStyle;
        private static GUIStyle textFieldStyle;
        private static GUIStyle descriptionBoxStyle;
        private static GUIStyle buttonStyle;
        
        private static GUIStyle iconStyle;

        private static GUIStyle missingNameStyle;
        private static GUIStyle missingNumberStyle;

        private static GUIStyle haveNameStyle;
        private static GUIStyle haveNumberStyle;

        // Textures
        
        public static Texture2D smallDescriptionPanel;
        public static Texture2D smallDescriptionPanelHover;
        public static Texture2D costPanel;

        // Public Functions

        public static void Draw(Blueprint inspectedBlueprint, float _windowX, float _windowY) {
            blueprint = inspectedBlueprint;
            windowX  = _windowX;
            windowY = _windowY;

            InitialiseStyles();
            GUI.Label(new Rect(windowX + 898, windowY + 124, 280, 40), "Blueprint Details", titleStyle);
            DrawNameBox();
            DrawIconBox();
            DrawDescriptionBox();
            DrawCostBox();
            DrawExportButton();
            DrawDeleteButton();
        }

        // Private Functions

        private static void InitialiseStyles() {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle() {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };

            headerStyle = new GUIStyle() {
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };

            textFieldStyle = new GUIStyle() {
                padding = new RectOffset(10, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white, background = BlueprintsLibraryGUI.textField },
                focused = { textColor = Color.white, background = BlueprintsLibraryGUI.textFieldActive },
            };

            descriptionBoxStyle = new GUIStyle() {
                padding = new RectOffset(5, 5, 5, 5),
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = Color.white, background = smallDescriptionPanel },
                focused = { textColor = Color.white, background = smallDescriptionPanelHover }
            };

            buttonStyle = new GUIStyle() {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow, background = BlueprintsLibraryGUI.border135 },
                hover = { textColor = Color.white, background = BlueprintsLibraryGUI.border135Hover }
            };

            iconStyle = new GUIStyle() { normal = { background = null } };

            missingNameStyle = new GUIStyle() { alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color32(255, 99, 71, 255) } };
            missingNumberStyle = new GUIStyle() { alignment = TextAnchor.MiddleRight, normal = { textColor = new Color32(255, 99, 71, 255) } };

            haveNameStyle = new GUIStyle() { alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.green } };
            haveNumberStyle = new GUIStyle() { alignment = TextAnchor.MiddleRight, normal = { textColor = Color.green } };
        }

        private static void DrawNameBox() {
            // Name Box
            GUI.Label(new Rect(windowX + 898, windowY + 159, 280, 40), "Name", headerStyle);
            blueprintName = GUI.TextField(new Rect(windowX + 898, windowY + 179, 280, 40), blueprintName, textFieldStyle);
            if (blueprintName != blueprintNameLastUpdate) {
                blueprint.name = blueprintName;
                BlueprintManager.UpdateBlueprint(blueprint);
            }

            blueprintNameLastUpdate = blueprintName;
        }

        private static void DrawIconBox() {
            GUI.Label(new Rect(windowX + 898, windowY + 229, 280, 40), "Icon Name", headerStyle);
            blueprintIconName = GUI.TextField(new Rect(windowX + 898, windowY + 249, 280, 40), blueprintIconName, textFieldStyle);
            if (blueprintIconName != blueprintIconNameLastUpdate) {
                blueprint.icon = blueprintIconName;
                BlueprintManager.UpdateBlueprint(blueprint);
            }

            blueprintIconNameLastUpdate = blueprintIconName;
        }
        
        private static void DrawDescriptionBox() {
            GUI.Label(new Rect(windowX + 898, windowY + 299, 280, 40), "Description", headerStyle);
            blueprintDescription = GUI.TextArea(new Rect(windowX + 898, windowY + 324, 280, 80), blueprintDescription, descriptionBoxStyle);

            if (blueprintDescription != blueprintDescriptionLastUpdate) {
                blueprint.description = blueprintDescription;
                BlueprintManager.UpdateBlueprint(blueprint);
            }

            blueprintDescriptionLastUpdate = blueprintDescription;
        }

        private static void DrawCostBox() {
            GUI.Label(new Rect(windowX + 898, windowY + 409, 280, 40), "Cost", headerStyle);
            GUI.Box(new Rect(windowX + 898, windowY + 434, 280, 169), "", new GUIStyle() { normal = { background = costPanel } });

            List<MachineCost> missing = blueprint.getCost().Where(cost => !cost.affordable).ToList();
            List<MachineCost> have = blueprint.getCost().Where(cost => cost.affordable).ToList();
            float maxHeight = 45 * (missing.Count + have.Count);
            scrollPos = GUI.BeginScrollView(new Rect(windowX + 898, windowY + 434, 280, 169), scrollPos, new Rect(windowX + 898, windowY + 434, 280, maxHeight), false, false);

            float currentY = windowY + 439;
            foreach (MachineCost cost in missing) DrawCostItem(cost, ref currentY);
            foreach (MachineCost cost in have) DrawCostItem(cost, ref currentY);

            GUI.EndScrollView();
        }

        private static void DrawCostItem(MachineCost cost, ref float currentY) {
            ResourceInfo info = SaveState.GetResInfoFromId(cost.resId);
            GUI.Box(new Rect(windowX + 903, currentY, 40, 40), ModUtils.GetImageForResource(info.displayName), iconStyle);
            GUI.Label(new Rect(windowX + 948, currentY, 215, 40), info.displayName, cost.affordable ? haveNameStyle : missingNameStyle);

            string haveTotalText = $"{Player.instance.inventory.GetResourceCount(cost.resId)} / {cost.count}";
            GUI.Label(new Rect(windowX + 943, currentY, 215, 40), haveTotalText, cost.affordable ? haveNumberStyle : missingNumberStyle);
            currentY += 45;
        }

        private static void DrawExportButton() {
            string exportText = timeSinceExportClicked < 2 ? "Copied!" : "Export";
            if (GUI.Button(new Rect(windowX + 898, windowY + 613, 135, 40), exportText, buttonStyle)) {
                GUIUtility.systemCopyBuffer = blueprint.ToJson();
                timeSinceExportClicked = 0;
            }
        }

        private static void DrawDeleteButton() {
            string deleteText = deleteClicksRemaining == 10 ? "Delete" : $"Delete ({deleteClicksRemaining})";
            if (GUI.Button(new Rect(windowX + 1043, windowY + 613, 135, 40), deleteText, buttonStyle)) {
                --deleteClicksRemaining;
                if (deleteClicksRemaining == 0) {
                    BlueprintManager.DeleteBlueprint(blueprint, true);
                    BlueprintsLibraryGUI.inspectedBlueprintId = -1;
                }
            }
        }
    }
}
