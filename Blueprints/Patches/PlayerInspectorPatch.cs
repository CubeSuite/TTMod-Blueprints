using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints.Patches
{
    internal class PlayerInspectorPatch
    {
        [HarmonyPatch(typeof(PlayerInspector), "LateUpdate")]
        [HarmonyPrefix]
        public static void getBuildableMask(PlayerInspector __instance) {
            if (AimingHelper.buildablesMask != null) return;

            FieldInfo collisionLayersInfo = __instance.GetType().GetField("collisionLayers", BindingFlags.Instance | BindingFlags.NonPublic);
            AimingHelper.buildablesMask = (LayerMask)collisionLayersInfo.GetValue(__instance);
        }
    }
}
