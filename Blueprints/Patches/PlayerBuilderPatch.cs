using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blueprints.Patches
{
    internal class PlayerBuilderPatch
    {
        [HarmonyPatch(typeof(PlayerBuilder), "ProcessInput")]
        [HarmonyPrefix]
        static bool BlockInput() {
            if (MachineCopier.isCopying || MachinePaster.isPasting || BuildQueue.queuedBuildings.Count != 0) {
                return false;
            }

            return true;
        }
    }
}
