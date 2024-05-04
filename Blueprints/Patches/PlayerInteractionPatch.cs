using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blueprints.Patches
{
    internal class PlayerInteractionPatch
    {
        [HarmonyPatch(typeof(PlayerInteraction), "get_canEnterEraseMode")]
        [HarmonyPostfix]
        static void BlockEraseMode(ref bool __result) {
            if (MachineCopier.isCopying || MachinePaster.isPasting || BuildQueue.queuedBuildings.Count != 0) {
                __result = false;
            }
        }
    }
}
