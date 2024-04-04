using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints
{
    public static class BlueprintsLibrary
    {
        // Objects & Variables
        public static string dataFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\TechtonicaBlueprintsData";
        public static string currentBlueprintFile = $"{dataFolder}\\CurrentBlueprint.json";
        public static string showFile = $"{dataFolder}\\Show.txt";
        public static string hideFile = $"{dataFolder}\\Hide.txt";
        public static string resumeFile = $"{dataFolder}\\Resume.txt";
        public static string pasteFile = $"{dataFolder}\\Paste.txt";
        public static bool isOpen = false;

        // Public Functions

        public static void start() {
            string uiProgramPath = $"{AppDomain.CurrentDomain.BaseDirectory}/TechtonicaBlueprints.exe";
            if(File.Exists(uiProgramPath)) {
                System.Diagnostics.Process.Start(uiProgramPath);
            }
            else {
                Debug.LogError($"Could not find {uiProgramPath}");
            }

            hide();
        }

        public static void show() {
            if (MachineCopier.isCopying || MachinePaster.isPasting) return;

            File.WriteAllText(showFile, "");
            isOpen = true;
            if (UIManager.instance != null) UIManager.instance.pauseMenu.Open();
        }

        public static void hide() {
            File.WriteAllText(hideFile, "");
            isOpen = false;
            if (UIManager.instance != null) UIManager.instance.pauseMenu.Close();
        }
    }
}
