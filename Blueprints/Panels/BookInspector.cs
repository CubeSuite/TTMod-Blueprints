using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints.Panels
{
    public class BookInspector
    {
        // Objects & Variables
        public static float timeSinceExportClicked = 0;
        public static int deleteClicksRemaining = 10;

        private static BlueprintBook book;
        private static float windowX;
        private static float windowY;

        // Field Values
        public static string bookName;
        public static string bookNameLastUpdate;
        
        public static string bookIconName;
        public static string bookIconNameLastUpdate;
        
        public static string bookDescription;
        public static string bookDescriptionLastUpdate;

        // Styles
        private static GUIStyle titleStyle;
        private static GUIStyle headerStyle;
        private static GUIStyle textFieldStyle;
        private static GUIStyle descriptionBoxStyle;
        private static GUIStyle buttonStyle;

        // Textures
        public static Texture2D bigDescriptionPanel;
        public static Texture2D bigDescriptionPanelHover;

        // Public Functions

        public static void Draw(BlueprintBook inspectedBook, float _windowX,  float _windowY) {
            book = inspectedBook;
            windowX = _windowX;
            windowY = _windowY;

            InitialiseStyles();
            GUI.Label(new Rect(windowX + 898, windowY + 124, 280, 40), "Book Details", titleStyle);
            DrawNameBox();
            DrawIconBox();
            DrawDescriptionBox();
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
                normal = { textColor = Color.white, background = bigDescriptionPanel },
                focused = { textColor = Color.white, background = bigDescriptionPanelHover }
            };

            buttonStyle = new GUIStyle() {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow, background = BlueprintsLibraryGUI.border135 },
                hover = { textColor = Color.white, background = BlueprintsLibraryGUI.border135Hover }
            };
        }

        private static void DrawNameBox() {
            GUI.Label(new Rect(windowX + 898, windowY + 159, 280, 40), "Name", headerStyle);
            bookName = GUI.TextField(new Rect(windowX + 898, windowY + 179, 280, 40), bookName, textFieldStyle);
            if (bookName != bookNameLastUpdate) {
                book.name = bookName;
                BookManager.UpdateBook(book);
            }

            bookNameLastUpdate = bookName;
        }

        private static void DrawIconBox() {
            GUI.Label(new Rect(windowX + 898, windowY + 229, 280, 40), "Icon Name", headerStyle);
            bookIconName = GUI.TextField(new Rect(windowX + 898, windowY + 249, 280, 40), bookIconName, textFieldStyle);
            if (bookIconName != bookIconNameLastUpdate) {
                book.icon = bookIconName;
                BookManager.UpdateBook(book);
            }

            bookIconNameLastUpdate = bookIconName;
        }

        private static void DrawDescriptionBox() {
            GUI.Label(new Rect(windowX + 898, windowY + 299, 280, 40), "Description", headerStyle);
            bookDescription = GUI.TextArea(new Rect(windowX + 898, windowY + 324, 280, 279), bookDescription, descriptionBoxStyle);

            if (bookDescription != bookDescriptionLastUpdate) {
                book.description = bookDescription;
                BookManager.UpdateBook(book);
            }

            bookDescriptionLastUpdate = bookDescription;
        }

        private static void DrawExportButton() {
            string exportText = timeSinceExportClicked < 2 ? "Copied!" : "Export";
            if (GUI.Button(new Rect(windowX + 898, windowY + 613, 135, 40), exportText, buttonStyle)) {
                SharableBook sharable = new SharableBook(book);
                GUIUtility.systemCopyBuffer = sharable.Serialise();
                timeSinceExportClicked = 0;
            }
        }

        private static void DrawDeleteButton() {
            string deleteText = deleteClicksRemaining == 10 ? "Delete" : $"Delete ({deleteClicksRemaining})";
            if (GUI.Button(new Rect(windowX + 1043, windowY + 613, 135, 40), deleteText, buttonStyle)) {
                --deleteClicksRemaining;
                if (deleteClicksRemaining == 0) {
                    BookManager.DeleteBook(book, true);
                    BlueprintsLibraryGUI.inspectedBookId = -1;
                }
            }
        }
    }
}
