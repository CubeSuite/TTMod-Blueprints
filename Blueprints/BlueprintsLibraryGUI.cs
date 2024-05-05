using AmplifyImpostors;
using BepInEx;
using EquinoxsModUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints
{
    public static class BlueprintsLibraryGUI
    {
        // Objects & Variables
        public static bool shouldShow = false;

        private const float windowWidth = 1200;
        private const float windowHeight = 675;
        private static float windowX => (Screen.width - windowWidth) / 2.0f;
        private static float windowY => (Screen.height - windowHeight) / 2.0f;

        #region Textures

        private static Texture2D shaderTile;
        private static Texture2D background;

        private static Texture2D border;
        private static Texture2D borderHover;

        #endregion

        // Public Functions

        public static void DrawGUI() {
            if (ShouldClose()) {
                CloseGUI();
                return;
            }

            DrawShader();
            DrawBackground();
            DrawTopButtons();
        }

        public static void CloseGUI() {
            shouldShow = false;
            ModUtils.FreeCursor(false);
        }

        public static void LoadTextures() {
            shaderTile = ModUtils.LoadTexture2DFromFile("Blueprints.Images.ShaderTile.png");
            background = ModUtils.LoadTexture2DFromFile("Blueprints.Images.LibraryBackground.png");

            border = ModUtils.LoadTexture2DFromFile("Blueprints.Images.Border200x40.png");
            borderHover = ModUtils.LoadTexture2DFromFile("Blueprints.Images.BorderHover200x40.png");
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
                normal = { textColor = Color.yellow, background = border },
                hover = { textColor = new Color(22, 22, 38), background = borderHover }
            };

            if (GUI.Button(new Rect(windowX + 373, windowY + 7, 200, 40), "New Blueprint", buttonStyle)) {
                if(BlueprintsPlugin.clipboard == null) {
                    BlueprintsPlugin.Notify("No Machines Copied");
                    CloseGUI();
                    return;
                }

                BlueprintManager.AddBlueprint(BlueprintsPlugin.clipboard);
            }

            if(GUI.Button(new Rect(windowX + 578, windowY + 7, 200, 40), "New Book", buttonStyle)) {
                // ToDo: Create New Book
            }

            if (GUI.Button(new Rect(windowX + 783, windowY + 7, 200, 40), "Import Blueprint", buttonStyle)) {
                // ToDo: Import Blueprint
            }

            if(GUI.Button(new Rect(windowX + 988, windowY + 7, 200, 40), "Import Book", buttonStyle)){
                // ToDo: Import Book
            }
        }
    }
}
