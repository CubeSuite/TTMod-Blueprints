using BepInEx.Configuration;
using EquinoxsModUtils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions.Must;
using static Rewired.Integration.UnityUI.RewiredPointerInputModule;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Blueprints
{
    public static class ControlsGUI
    {
        // Objects & Variables

        private static List<DynamicControl> controlsToRender = new List<DynamicControl>();

        // Textures

        private static Texture2D shaderTile;
        private static Dictionary<KeyCode, Texture2D> keyTextures = new Dictionary<KeyCode, Texture2D>();

        // Styles

        private static GUIStyle labelStyle;

        // Public Functions

        public static void DrawControlsGUI() {
            InitialiseStyles();

            if (!EMU.LoadingStates.hasGameLoaded) return;
            if (BlueprintsLibraryGUI.shouldShow) return;

            if (BlueprintsPlugin.machinesToCopy.Count != 0) return;
            if (BuildQueue.queuedBuildings.Count != 0) return;

            RefreshControls();

            for(int i = 0; i < controlsToRender.Count; i++) {
                if (controlsToRender[i] == null) continue;
                DrawControl(controlsToRender[i], i);
            }
        }

        // Private Functions

        private static void InitialiseStyles() {
            if (labelStyle != null) return;

            #region Key Textures

            keyTextures.Add(KeyCode.Semicolon, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.;.png"));
            keyTextures.Add(KeyCode.LeftBracket, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.[.png"));
            keyTextures.Add(KeyCode.RightBracket, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.].png"));
            keyTextures.Add(KeyCode.Alpha0, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.0.png"));
            keyTextures.Add(KeyCode.Alpha1, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.1.png"));
            keyTextures.Add(KeyCode.Alpha2, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.2.png"));
            keyTextures.Add(KeyCode.Alpha3, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.3.png"));
            keyTextures.Add(KeyCode.Alpha4, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.4.png"));
            keyTextures.Add(KeyCode.Alpha5, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.5.png"));
            keyTextures.Add(KeyCode.Alpha6, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.6.png"));
            keyTextures.Add(KeyCode.Alpha7, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.7.png"));
            keyTextures.Add(KeyCode.Alpha8, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.8.png"));
            keyTextures.Add(KeyCode.Alpha9, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.9.png"));
            keyTextures.Add(KeyCode.F1, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F1.png"));
            keyTextures.Add(KeyCode.F2, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F2.png"));
            keyTextures.Add(KeyCode.F3, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F3.png"));
            keyTextures.Add(KeyCode.F4, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F4.png"));
            keyTextures.Add(KeyCode.F5, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F5.png"));
            keyTextures.Add(KeyCode.F6, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F6.png"));
            keyTextures.Add(KeyCode.F7, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F7.png"));
            keyTextures.Add(KeyCode.F8, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F8.png"));
            keyTextures.Add(KeyCode.F9, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F9.png"));
            keyTextures.Add(KeyCode.F10, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F10.png"));
            keyTextures.Add(KeyCode.F11, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F11.png"));
            keyTextures.Add(KeyCode.F12, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F12.png"));
            keyTextures.Add(KeyCode.A, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.A.png"));
            keyTextures.Add(KeyCode.B, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.B.png"));
            keyTextures.Add(KeyCode.C, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.C.png"));
            keyTextures.Add(KeyCode.D, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.D.png"));
            keyTextures.Add(KeyCode.E, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.E.png"));
            keyTextures.Add(KeyCode.F, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.F.png"));
            keyTextures.Add(KeyCode.G, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.G.png"));
            keyTextures.Add(KeyCode.H, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.H.png"));
            keyTextures.Add(KeyCode.I, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.I.png"));
            keyTextures.Add(KeyCode.J, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.J.png"));
            keyTextures.Add(KeyCode.K, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.K.png"));
            keyTextures.Add(KeyCode.L, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.L.png"));
            keyTextures.Add(KeyCode.M, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.M.png"));
            keyTextures.Add(KeyCode.N, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.N.png"));
            keyTextures.Add(KeyCode.O, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.O.png"));
            keyTextures.Add(KeyCode.P, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.P.png"));
            keyTextures.Add(KeyCode.Q, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Q.png"));
            keyTextures.Add(KeyCode.R, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.R.png"));
            keyTextures.Add(KeyCode.S, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.S.png"));
            keyTextures.Add(KeyCode.T, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.T.png"));
            keyTextures.Add(KeyCode.U, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.U.png"));
            keyTextures.Add(KeyCode.V, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.V.png"));
            keyTextures.Add(KeyCode.W, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.W.png"));
            keyTextures.Add(KeyCode.X, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.X.png"));
            keyTextures.Add(KeyCode.Y, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Y.png"));
            keyTextures.Add(KeyCode.Z, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Z.png"));
            keyTextures.Add(KeyCode.UpArrow, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Up.png"));
            keyTextures.Add(KeyCode.DownArrow, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Down.png"));
            keyTextures.Add(KeyCode.LeftArrow, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Left.png"));
            keyTextures.Add(KeyCode.RightArrow, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Right.png"));
            keyTextures.Add(KeyCode.LeftAlt, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Alt.png"));
            keyTextures.Add(KeyCode.LeftShift, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Shift.png"));
            keyTextures.Add(KeyCode.LeftControl, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Control.png"));
            keyTextures.Add(KeyCode.RightAlt, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Alt.png"));
            keyTextures.Add(KeyCode.RightShift, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Shift.png"));
            keyTextures.Add(KeyCode.RightControl, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Control.png"));
            keyTextures.Add(KeyCode.Backslash, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Backslash.png"));
            keyTextures.Add(KeyCode.Backspace, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Backspace.png"));
            keyTextures.Add(KeyCode.CapsLock, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Capslock.png"));
            keyTextures.Add(KeyCode.Delete, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Delete.png"));
            keyTextures.Add(KeyCode.End, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.End.png"));
            keyTextures.Add(KeyCode.Return, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Enter.png"));
            keyTextures.Add(KeyCode.KeypadEnter, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Keypad_Enter.png"));
            keyTextures.Add(KeyCode.Slash, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Forwardslash.png"));
            keyTextures.Add(KeyCode.Greater, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.GreaterThan.png"));
            keyTextures.Add(KeyCode.Home, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Home.png"));
            keyTextures.Add(KeyCode.Insert, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Insert.png"));
            keyTextures.Add(KeyCode.Less, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.LessThan.png"));
            keyTextures.Add(KeyCode.Minus, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Minus.png"));
            keyTextures.Add(KeyCode.KeypadMinus, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Minus.png"));
            keyTextures.Add(KeyCode.KeypadMultiply, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Multiply.png"));
            keyTextures.Add(KeyCode.Numlock, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Numlock.png"));
            keyTextures.Add(KeyCode.PageDown, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.PageDown.png"));
            keyTextures.Add(KeyCode.PageUp, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.PageUp.png"));
            keyTextures.Add(KeyCode.Plus, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Plus.png"));
            keyTextures.Add(KeyCode.Print, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.PrintScreen.png"));
            keyTextures.Add(KeyCode.Question, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Question.png"));
            keyTextures.Add(KeyCode.Quote, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Quotation.png"));
            keyTextures.Add(KeyCode.Space, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Space.png"));
            keyTextures.Add(KeyCode.Tab, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Tab.png"));
            keyTextures.Add(KeyCode.Tilde, EMU.Images.LoadTexture2DFromFile($"Blueprints.Images.Keys.Tilde.png"));

            #endregion

            labelStyle = new GUIStyle() {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.yellow }
            };

            shaderTile = EMU.Images.LoadTexture2DFromFile("Blueprints.Images.ShaderTile.png");
        }

        private static void RefreshControls() {
            controlsToRender.Clear();

            if(!MachineCopier.isCopying && !MachinePaster.isPasting) {
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.copyShortcut, "Start Copying"));

                if (BlueprintsPlugin.clipboard != null) {
                    controlsToRender.Add(new DynamicControl(BlueprintsPlugin.pasteShortcut, "Start Pasting"));
                }

                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.blueprintsShortcut, "Open Blueprints Library"));
            }
            else if (MachineCopier.isCopying) {
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.copyShortcut, "Finish Copying"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.cancelShortcut, "Cancel Copying"));

                controlsToRender.Add(null);

                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeForwardShortcut, "Expand Forwards"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeBackwardShortcut, "Expand Backwards"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeLeftShortcut, "Expand Left"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeRightShortcut, "Expand Right"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeUpShortcut, "Expand Up"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeDownShortcut, "Expand Down"));

                controlsToRender.Add(null);

                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.shrinkForwardShortcut, "Shrink Forwards"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.shrinkBackwardShortcut, "Shrink Backwards"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.shrinkLeftShortcut, "Shrink Left"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.shrinkRightShortcut, "Shrink Right"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.shrinkUpShortcut, "Shrink Up"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.shrinkDownShortcut, "Shrink Down"));

            }
            else if (MachinePaster.isPasting) {
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.pasteShortcut, "Place Blueprint"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.cancelShortcut, "Cancel Pasting"));
                
                controlsToRender.Add(null);
                
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.cwRotateShortcut, "Rotate CW"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.ccwRotateShortcut, "Rotate CCW"));

                controlsToRender.Add(null);

                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.lockPositionShortcut, "Lock Mouse Aiming"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeForwardShortcut, "Nudge Forwards"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeBackwardShortcut, "Nudge Backwards"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeLeftShortcut, "Nudge Left"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeRightShortcut, "Nudge Right"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeUpShortcut, "Nudge Up"));
                controlsToRender.Add(new DynamicControl(BlueprintsPlugin.nudgeDownShortcut, "Nudge Down"));
            }

            //controlsToRender.Reverse();
        }

        private static void DrawControl(DynamicControl control, int index) {
            // Keybind

            float yPos = Screen.height - ((controlsToRender.Count - index) * 45);
            float cummulativeWidth = 5;

            for (int i = control.keyCodes.Length - 1; i >= 0; i--) {
                Texture2D keyTexture = keyTextures[control.keyCodes[i]];
                int keyWidth = (int)(keyTexture.width * (40.0 / keyTexture.height));
                cummulativeWidth += keyWidth + 5;
                
                GUIStyle imageStyle = new GUIStyle() { normal = { background = keyTexture } };
                GUI.Box(new Rect(Screen.width - cummulativeWidth, yPos, keyWidth, 40), "", imageStyle);
            }

            // Action
            float width = labelStyle.CalcSize(new GUIContent(control.action)).x;
            float xPos = Screen.width - cummulativeWidth - width - 10;

            GUI.Label(new Rect(xPos, yPos, width, 40), control.action, labelStyle);
        }
    }

    public class DynamicControl {
        public KeyCode[] keyCodes;
        public string action;

        // Constructors

        public DynamicControl(KeyCode[] _keyCodes, string _action) {
            keyCodes = _keyCodes;
            action = _action;
        }

        public DynamicControl(KeyCode keyCode, string _action) {
            keyCodes = new KeyCode[] { keyCode };
            action = _action;
        }

        public DynamicControl(KeyCode[] modifiers, KeyCode keyCode, string _action) {
            KeyCode[] _keyCodes = new KeyCode[modifiers.Length + 1];
            for(int i = 0; i < modifiers.Length; i++) {
                _keyCodes[i] = modifiers[i];
            }

            _keyCodes[_keyCodes.Length - 1] = keyCode;
            keyCodes = _keyCodes;

            action = _action;
        }

        public DynamicControl(ConfigEntry<KeyboardShortcut> configEntry, string _action) {
            action = _action;
            if(configEntry.Value.Modifiers == null) {
                keyCodes = new KeyCode[] { configEntry.Value.MainKey };
                return;
            }

            KeyCode[] modifiers = configEntry.Value.Modifiers.ToArray();
            
            KeyCode[] _keyCodes = new KeyCode[modifiers.Length + 1];
            for (int i = 0; i < modifiers.Length; i++) {
                _keyCodes[i] = modifiers[i];
            }

            _keyCodes[_keyCodes.Length - 1] = configEntry.Value.MainKey;
            keyCodes = _keyCodes;
        }
    }
}
