using AmplifyImpostors;
using BepInEx;
using Blueprints.Panels;
using EquinoxsModUtils;
using SoftMasking;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Rewired.Integration.UnityUI.RewiredPointerInputModule;

namespace Blueprints
{
    public static class BlueprintsLibraryGUI
    {
        // Objects & Variables
        public static bool shouldShow = false;
        public static int inspectedBlueprintId = -1;
        public static int inspectedBookId = -1;

        private const float windowWidth = 1200;
        private const float windowHeight = 675;
        private static float windowX => (Screen.width - windowWidth) / 2.0f;
        private static float windowY => (Screen.height - windowHeight) / 2.0f;
        private static float timeSinceExportAllClicked = 0;

        // Textures
        private static Texture2D shaderTile;
        private static Texture2D background;
        private static Texture2D yellowPixel;

        public static Texture2D border135;
        public static Texture2D border135Hover;

        public static Texture2D textField;
        public static Texture2D textFieldActive;

        // Public Functions

        public static void DrawGUI() {
            BlueprintInspector.timeSinceExportClicked += Time.deltaTime;
            BookInspector.timeSinceExportClicked += Time.deltaTime;
            timeSinceExportAllClicked += Time.deltaTime;

            if (ShouldClose()) {
                CloseGUI();
                return;
            }

            DrawShader();
            DrawBackground();
            DrawTopButtons();
            DrawInspectedPanel();
            BookPanel.DrawCurrentBook(windowX, windowY);
        }

        public static void CloseGUI() {
            shouldShow = false;
            ModUtils.FreeCursor(false);
            inspectedBlueprintId = -1;
            inspectedBookId = -1;
        }

        public static void LoadTextures() {
            shaderTile = ModUtils.LoadTexture2DFromFile("Blueprints.Images.ShaderTile.png");
            background = ModUtils.LoadTexture2DFromFile("Blueprints.Images.LibraryBackground.png");
            yellowPixel = ModUtils.LoadTexture2DFromFile("Blueprints.Images.YellowPixel.png");

            BookPanel.border122 = ModUtils.LoadTexture2DFromFile("Blueprints.Images.Border122x40.png");
            BookPanel.border122Hover = ModUtils.LoadTexture2DFromFile("Blueprints.Images.BorderHover122x40.png");
            border135 = ModUtils.LoadTexture2DFromFile("Blueprints.Images.Border135x40.png");
            border135Hover = ModUtils.LoadTexture2DFromFile("Blueprints.Images.BorderHover135x40.png");

            textField = ModUtils.LoadTexture2DFromFile("Blueprints.Images.TextField.png");
            textFieldActive = ModUtils.LoadTexture2DFromFile("Blueprints.Images.TextFieldActive.png");

            BookInspector.bigDescriptionPanel = ModUtils.LoadTexture2DFromFile("Blueprints.Images.BigDescriptionPanel.png");
            BookInspector.bigDescriptionPanelHover = ModUtils.LoadTexture2DFromFile("Blueprints.Images.BigDescriptionPanelHover.png");
            BlueprintInspector.smallDescriptionPanel = ModUtils.LoadTexture2DFromFile("Blueprints.Images.SmallDescriptionPanel.png");
            BlueprintInspector.smallDescriptionPanelHover = ModUtils.LoadTexture2DFromFile("Blueprints.Images.SmallDescriptionPanelHover.png");

            BookPanel.slotPanel = ModUtils.LoadTexture2DFromFile("Blueprints.Images.SlotPanel.png");
            BlueprintInspector.costPanel = ModUtils.LoadTexture2DFromFile("Blueprints.Images.CostPanel.png");
        }

        // Private Functions

        private static bool ShouldClose() {
            return (UnityInput.Current.GetKeyDown(KeyCode.Escape) ||
                    UnityInput.Current.GetKeyDown(KeyCode.Tab) ||
                    UnityInput.Current.GetKeyDown(KeyCode.Q) ||
                    UnityInput.Current.GetKeyDown(KeyCode.E));
        }

        // Draw Functions

        private static void DrawShader() {
            GUIStyle shaderStyle = new GUIStyle() { normal = { background = shaderTile } };
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", shaderStyle);
        }

        private static void DrawBackground() {
            GUIStyle shaderStyle = new GUIStyle() { normal = { background = background } };
            GUI.Box(new Rect(windowX, windowY, windowWidth, windowHeight), "", shaderStyle);
        }

        private static void DrawTopButtons() {
            GUIStyle buttonStyle = new GUIStyle {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow, background = border135 },
                hover = { textColor = new Color(22, 22, 38), background = border135Hover }
            };

            float width = 135;

            if (GUI.Button(new Rect(windowX + 493, windowY + 7, width, 40), "New Blueprint", buttonStyle)) {
                if(BlueprintsPlugin.clipboard == null) {
                    BlueprintsPlugin.Notify("No Machines Copied");
                    CloseGUI();
                    return;
                }

                Blueprint blueprint = Blueprint.CreateFromClipboard();

                int newBlueprintID = BlueprintManager.AddBlueprint(blueprint);
                blueprint.id = newBlueprintID;
                BlueprintBook book = BookManager.GetCurrentBook();
                book.AddBlueprint(newBlueprintID);
                BookManager.UpdateBook(book);

                blueprint.parentId = book.id;
                BlueprintManager.UpdateBlueprint(blueprint);
            }

            if(GUI.Button(new Rect(windowX + 633, windowY + 7, width, 40), "New Book", buttonStyle)) {
                BlueprintBook newBook = new BlueprintBook() { parentId = BookManager.currentBookId };
                BookManager.AddBook(newBook);

                BlueprintBook currentBook = BookManager.GetCurrentBook();
                currentBook.AddBook(newBook);
                BookManager.UpdateBook(currentBook);
            }

            if (GUI.Button(new Rect(windowX + 773, windowY + 7, width, 40), "Import Blueprint", buttonStyle)) {
                string json = GUIUtility.systemCopyBuffer;
                try {
                    Blueprint blueprint = (Blueprint)JsonUtility.FromJson(json, typeof(Blueprint));
                    blueprint.id = -1;
                    int newID = BlueprintManager.AddBlueprint(blueprint);

                    BlueprintBook book = BookManager.GetCurrentBook();
                    book.AddBlueprint(newID);
                    BookManager.UpdateBook(book);
                }
                catch (Exception e) {
                    Player.instance.audio.buildError.PlayRandomClip();
                    BlueprintsPlugin.Log.LogError($"Error occurred while parsing blueprint json: {e.Message}");
                }
            }

            if(GUI.Button(new Rect(windowX + 913, windowY + 7, width, 40), "Import Book", buttonStyle)){
                string xml = GUIUtility.systemCopyBuffer;
                try {
                    SharableBook book = SharableBook.Parse(xml);
                    int newBookID = BookManager.AddSharableBook(book, BookManager.currentBookId);

                    BlueprintBook currentBook = BookManager.GetCurrentBook();
                    currentBook.AddBook(newBookID);
                    BookManager.UpdateBook(currentBook);
                }
                catch (Exception e) {
                    Player.instance.audio.buildError.PlayRandomClip();
                    BlueprintsPlugin.Log.LogError($"Error occurred while parsing book xml: {e.Message}");
                }
            }

            string exportText = timeSinceExportAllClicked < 2 ? "Copied!" : "Export All";
            if(GUI.Button(new Rect(windowX + 1053, windowY + 7, 135, 40), exportText, buttonStyle)) {
                SharableBook book = new SharableBook(BookManager.GetRootBook());
                GUIUtility.systemCopyBuffer = book.Serialise();
            }
        }

        private static void DrawInspectedPanel() {
            GUIStyle titleStyle = new GUIStyle() {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };
            Rect titleRect = new Rect(windowX + 898, windowY + 124, 280, 40);
            GUI.Box(new Rect(windowX + 898, windowY + 144, 280, 2), "", new GUIStyle() { normal = { background = yellowPixel } });

            if (inspectedBlueprintId != -1) {
                Blueprint inspectedBlueprint = BlueprintManager.TryGetBlueprint(inspectedBlueprintId);
                BlueprintInspector.Draw(inspectedBlueprint, windowX, windowY);
            }
            else if (inspectedBookId != -1) {
                BlueprintBook inspectedBook = BookManager.TryGetBook(inspectedBookId);
                BookInspector.Draw(inspectedBook, windowX, windowY);
            }
            else {
                GUI.Label(titleRect, "Click a book or blueprint to view its details.", titleStyle);
            }
        }
    }
}
