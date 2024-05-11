using BepInEx;
using EquinoxsModUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Voxeland5.PosTab;

namespace Blueprints.Panels
{
    public static class BookPanel
    {
        // Objects & Variables

        private static float windowX;
        private static float windowY;

        // Styles
        private static GUIStyle panelStyle;
        private static GUIStyle labelStyle;
        private static GUIStyle buttonStyle;
        private static GUIStyle pathStyle;

        // Textures
        public static Texture2D slotPanel;
        public static Texture2D border122;
        public static Texture2D border122Hover;

        // Public Functions

        public static void DrawCurrentBook(float _windowX, float _windowY) {
            windowX = _windowX;
            windowY = _windowY;

            InitialiseStyles();
            DrawPath();
            DrawBackButton();
            DrawChildBooks();
            DrawBlueprints();
        }

        // Private Functions

        private static void InitialiseStyles() {
            if (panelStyle != null) return;

            panelStyle = new GUIStyle() { normal = { background = slotPanel } };
            
            labelStyle = new GUIStyle() {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };

            buttonStyle = new GUIStyle() {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow, background = border122 },
                hover = { textColor = new Color(22, 22, 38), background = border122Hover }
            };

            pathStyle = new GUIStyle() {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
        }

        private static void DrawPath() {
            string path = BookManager.GetCurrentBook().GetPath();
            GUI.Label(new Rect(windowX + 22, windowY + 64, 1156, 40), path, pathStyle);
        }

        private static void DrawBackButton() {
            if (BookManager.currentBookId == 0) return;

            if (GUI.Button(new Rect(windowX + 1066, windowY + 64, 122, 40), "Back", buttonStyle)) {
                BookManager.currentBookId = BookManager.GetCurrentBook().parentId;
                BlueprintsLibraryGUI.inspectedBlueprintId = -1;
                BlueprintsLibraryGUI.inspectedBookId = -1;
                BlueprintInspector.deleteClicksRemaining = 10;
                BookInspector.deleteClicksRemaining = 10;
            }
        }

        private static void DrawChildBooks() {
            List<Slot> childBookSlots = BookManager.GetCurrentBook().GetSlots().Where(slot => slot.GetSlotType() == SlotType.Book).ToList();
            if (childBookSlots.Count == 0) return;

            for (int i = 0; i < childBookSlots.Count; i++) {
                Slot slot = childBookSlots[i];
                BlueprintBook book = BookManager.TryGetBook(slot.bookId);
                if (book == null) {
                    BlueprintsPlugin.Log.LogError("Can't draw child book, is null");
                    continue;
                }

                float xPos = windowX + 12f + 10f + (i % 6f * 142f);
                float yPos = windowY + 114f + 10f + (Mathf.FloorToInt(i / 6.0f) * 142f);
                DrawChildBookInSlot(book, xPos, yPos);
            }
        }

        private static void DrawChildBookInSlot(BlueprintBook book, float slotX, float slotY) {
            GUI.Box(new Rect(slotX, slotY, 132, 132), "", panelStyle);
            HandleBookPanelClicked(book, slotX, slotY);

            GUI.Label(new Rect(slotX + 10, slotY + 5, 112, 40), book.name, labelStyle);

            if (ResourceNames.SafeResources.Contains(book.icon)) {
                GUI.Box(new Rect(slotX + 36, slotY + 20, 60, 60), "", new GUIStyle() {
                    normal = { background = ModUtils.GetImageForResource(book.icon) }
                });
            }

            if (GUI.Button(new Rect(slotX + 5, slotY + 87, 122, 40), "Open", buttonStyle)) {
                BookManager.currentBookId = book.id;
                BlueprintsLibraryGUI.inspectedBlueprintId = -1;
                BlueprintsLibraryGUI.inspectedBookId = -1;
            }
        }

        private static void HandleBookPanelClicked(BlueprintBook book, float slotX, float slotY) {
            if (!UnityInput.Current.GetMouseButtonDown(0)) return;

            float mouseX = UnityInput.Current.mousePosition.x;
            float mouseY = Screen.height - UnityInput.Current.mousePosition.y;

            // Overlapping GUI.Button's don't work, so panel click detection has to be handled manually
            // Close your eyes before looking at this code.

            // See if click is in panel
            if (mouseX > slotX && mouseX < slotX + 132 &&
                mouseY > slotY && mouseY < slotY + 132) {

                // See if click is not in 'Open' button
                if (mouseX < slotX + 5 || mouseX > slotX + 127 ||
                    mouseY < slotY + 87 || mouseY > slotY + 127) {

                    // Panel was clicked outside of 'Open' button
                    BlueprintsLibraryGUI.inspectedBlueprintId = -1;
                    BlueprintsLibraryGUI.inspectedBookId = book.id;
                    BookInspector.deleteClicksRemaining = 10;

                    BookInspector.bookName = book.name;
                    BookInspector.bookNameLastUpdate = book.name;

                    BookInspector.bookIconName = book.icon;
                    BookInspector.bookIconNameLastUpdate = book.icon;

                    BookInspector.bookDescription = book.description;
                    BookInspector.bookDescriptionLastUpdate = book.description;
                }
            }
        }

        private static void DrawBlueprints() {
            int numBooks = BookManager.GetCurrentBook().GetSlots().Where(slot => slot.GetSlotType() == SlotType.Book).Count();
            List<Slot> childBlueprintSlots = BookManager.GetCurrentBook().GetSlots().Where(slot => slot.GetSlotType() == SlotType.Blueprint).ToList();
            if (childBlueprintSlots.Count == 0) return;

            for (int i = 0; i < childBlueprintSlots.Count; i++) {
                Slot slot = childBlueprintSlots[i];
                Blueprint blueprint = BlueprintManager.TryGetBlueprint(slot.blueprintId);
                if (blueprint == null) {
                    BlueprintsPlugin.Log.LogError("Can't draw child blueprint, is null");
                    continue;
                }

                float xPos = windowX + 12f + 10f + ((i + numBooks) % 6f * 142f);
                float yPos = windowY + 114f + 10f + (Mathf.FloorToInt((i + numBooks) / 6.0f) * 142f);
                DrawBlueprintInSlot(blueprint, xPos, yPos);
            }
        }

        private static void DrawBlueprintInSlot(Blueprint blueprint, float slotX, float slotY) {
            GUI.Box(new Rect(slotX, slotY, 132, 132), "", panelStyle);
            
            HandleBlueprintPanelClicked(blueprint, slotX, slotY);

            GUI.Label(new Rect(slotX + 10, slotY + 5, 112, 40), blueprint.name, labelStyle);

            if (ResourceNames.SafeResources.Contains(blueprint.icon)) {
                GUI.Box(new Rect(slotX + 36, slotY + 20, 60, 60), "", new GUIStyle() {
                    normal = { background = ModUtils.GetImageForResource(blueprint.icon) }
                });
            }

            if (GUI.Button(new Rect(slotX + 5, slotY + 87, 122, 40), "Use", buttonStyle)) {
                if (!blueprint.CanAfford()) {
                    Player.instance.audio.buildError.PlayRandomClip();
                    return;
                }

                blueprint.SetSize(blueprint.GetSize().AsUnityVector3());
                BlueprintsPlugin.clipboard = blueprint;
                MachinePaster.StartPasting();
                BlueprintsLibraryGUI.CloseGUI();
            }
        }

        private static void HandleBlueprintPanelClicked(Blueprint blueprint, float slotX, float slotY) {
            // Overlapping GUI.Button's don't work, so panel click detection has to be handled manually
            // Close your eyes before looking at this code.

            if (!UnityInput.Current.GetMouseButtonDown(0)) return;

            float mouseX = UnityInput.Current.mousePosition.x;
            float mouseY = Screen.height - UnityInput.Current.mousePosition.y;

            // See if click is in panel
            if (mouseX > slotX && mouseX < slotX + 132 &&
                mouseY > slotY && mouseY < slotY + 132) {

                // See if click is not in 'Use' button
                if (mouseX < slotX + 5 || mouseX > slotX + 127 ||
                    mouseY < slotY + 87 || mouseY > slotY + 127) {

                    // Panel was clicked outside of 'Use' button
                    BlueprintsLibraryGUI.inspectedBookId = -1;
                    BlueprintsLibraryGUI.inspectedBlueprintId = blueprint.id;
                    BlueprintInspector.deleteClicksRemaining = 10;

                    BlueprintInspector.blueprintName = blueprint.name;
                    BlueprintInspector.blueprintNameLastUpdate = blueprint.name;

                    BlueprintInspector.blueprintIconName = blueprint.icon;
                    BlueprintInspector.blueprintIconNameLastUpdate = blueprint.icon;

                    BlueprintInspector.blueprintDescription = blueprint.description;
                    BlueprintInspector.blueprintDescriptionLastUpdate = blueprint.description;
                }
            }
        }
    }
}
