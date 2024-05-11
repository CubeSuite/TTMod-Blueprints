using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints
{
    public static class BlueprintManager
    {
        // Objects & Variables
        private static Dictionary<int, Blueprint> blueprints = new Dictionary<int, Blueprint>();
        private static string saveFile = $"{Application.persistentDataPath}/Blueprints.json";

        // Public Functions

        public static int AddBlueprint(Blueprint blueprint, bool shouldSave = true) {
            if (blueprint.id < 0) blueprint.id = GetNewBlueprintID();
            blueprints.Add(blueprint.id, blueprint);
            if (shouldSave) SaveData();

            return blueprint.id;
        }

        public static void UpdateBlueprint(Blueprint blueprint) {
            if (DoesBlueprintExist(blueprint)) {
                blueprints[blueprint.id] = blueprint;
                SaveData();
            }
        }

        public static bool DoesBlueprintExist(int id) {
            return blueprints.ContainsKey(id);
        }

        public static Blueprint TryGetBlueprint(int id) {
            if (DoesBlueprintExist(id)) return blueprints[id];
            else return null;
        }

        public static List<Blueprint> GetAllBlueprints() {
            return blueprints.Values.ToList();
        }

        public static void DeleteBlueprint(Blueprint blueprint, bool removeFromParent = false) {
            if(!DoesBlueprintExist(blueprint)) return;

            if(removeFromParent && BookManager.DoesBookExist(blueprint.parentId)) {
                Debug.Log($"Deleting from parent");
                BlueprintBook parent = BookManager.TryGetBook(blueprint.parentId);
                if (parent != null) {
                    Debug.Log($"Got parent");
                    parent.RemoveBlueprint(blueprint);
                    BookManager.UpdateBook(parent);
                    Debug.Log($"Removed from parent");
                }
            }

            blueprints.Remove(blueprint.id);
            SaveData();
        }

        // Private Functions

        private static int GetNewBlueprintID() {
            if (blueprints.Count == 0) return 0;
            else return blueprints.Keys.Max() + 1;
        }

        // Data Functions

        public static void SaveData() {
            List<string> jsons = new List<string>();
            foreach(Blueprint blueprint in blueprints.Values) {
                jsons.Add(blueprint.ToJson());
            }

            File.WriteAllLines(saveFile, jsons);
        }

        public static void LoadData() {
            if (!File.Exists(saveFile)) {
                BlueprintsPlugin.Log.LogWarning("Blueprints.json not found");
                return;
            }

            string[] jsons = File.ReadAllLines(saveFile);
            foreach(string json in jsons) {
                AddBlueprint((Blueprint)JsonUtility.FromJson(json, typeof(Blueprint)), false);
            }
        }

        #region Overloads

        public static bool DoesBlueprintExist(Blueprint blueprint) {
            return DoesBlueprintExist(blueprint.id);
        }

        public static void DeleteBlueprint(int id) {
            DeleteBlueprint(TryGetBlueprint(id));
        }

        #endregion
    }
}
