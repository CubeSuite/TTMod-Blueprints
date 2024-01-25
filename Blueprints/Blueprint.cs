using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Blueprints
{
    public class Blueprint
    {
        public int id;
        public string name;
        public List<IMachineInstanceRef> machines = new List<IMachineInstanceRef>();
        public List<Vector3> machineRelativePositions = new List<Vector3>();

        // Public Functions

        public string toJson(bool formatted = false) {
            return JsonUtility.ToJson(this, formatted);
        }

        public List<MachineCost> getCost() {
            List<MachineCost> cost = new List<MachineCost>();
            Dictionary<int, int> machineCounts = new Dictionary<int, int>();

            foreach(IMachineInstanceRef machine in machines) {
                int resId = machine.GetCommonInfo().resId;
                if (machineCounts.ContainsKey(resId)){
                    ++machineCounts[resId];
                }
                else {
                    machineCounts[resId] = 1;
                }
            }

            foreach(KeyValuePair<int, int> pair in  machineCounts) {
                cost.Add(new MachineCost(pair.Key, pair.Value));
            }

            return cost;
        }
    }

    public struct MachineCost {
        public int resId;
        public int count;

        public MachineCost(int _resId, int _count) {
            resId = _resId;
            count = _count;
        }
    }
}
