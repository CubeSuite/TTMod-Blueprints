using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints.Patches
{
    internal class PauseMenuPatch
    {
        [HarmonyPatch(typeof(PauseMenu), "Close")]
        [HarmonyPrefix]
        static void markUIClosed() {
            UI.isOpen = false;
            Debug.Log("Marked ui closed");
        }
    }
}
