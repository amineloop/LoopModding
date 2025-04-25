using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;
using System.IO;
using LoopModding.Core.API;

namespace LoopModding.Core
{
    public class ModManager : MonoBehaviour
    {
        public static ModManager Instance;

        private Dictionary<string, List<ModDefinition>> eventMap = new();
        private Dictionary<string, JSONNode> parameters = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAllParameters();
            LoadAllMods();
        }

          private void LoadAllParameters()
        {
            string path = Path.Combine(Application.dataPath, "../Mods/Parameters/");
            if (!Directory.Exists(path))
            {
                Debug.LogWarning("[ModManager] No parameters folder found.");
                return;
            }

            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                var node = JSON.Parse(json);

                if (node == null || !node.IsObject) continue;

                foreach (var kvp in node.AsObject)
                {
                    parameters[kvp.Key] = kvp.Value;
                    Debug.Log($"[ModManager] Loaded param: {kvp.Key} = {kvp.Value}");
                }
            }
        }

        private void LoadAllMods()
        {
            string path = Path.Combine(Application.dataPath, "../Mods/Addons/");
            if (!Directory.Exists(path))
            {
                Debug.LogWarning("[ModManager] No Mods/Addons folder found.");
                return;
            }

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                var node = JSON.Parse(json);
                if (node == null) continue;

                ModDefinition mod = new ModDefinition
                {
                    modName = node["modName"],
                    eventName = node["eventName"],
                    action = node["action"],
                    args = node["args"]
                };

                if (mod.args != null)
                    ResolveParameterReferences(mod.args);

                if (!eventMap.ContainsKey(mod.eventName))
                    eventMap[mod.eventName] = new List<ModDefinition>();

                eventMap[mod.eventName].Add(mod);
                Debug.Log($"[ModManager] Loaded mod: {mod.modName} for event {mod.eventName}");
            }
        }

        private void ResolveParameterReferences(JSONNode args)
        {
            List<string> keysToCheck = new List<string>();

            foreach (var key in args.Keys)
            {
                keysToCheck.Add(key); // 🔁 on clone manuellement les clés
            }

            foreach (var key in keysToCheck)
            {
                if (args[key] is JSONString str && str.Value.StartsWith("@"))
                {
                    string refKey = str.Value.Substring(1);
                    if (parameters.TryGetValue(refKey, out var resolvedValue))
                    {
                        args[key] = resolvedValue;
                        Debug.Log($"[ModManager] Resolved '@{refKey}' → {resolvedValue}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ModManager] Missing param '@{refKey}'");
                    }
                }
            }
        }

        public void ReloadFolders()
        {
            Debug.Log("[ModManager] Reloading mods and parameters...");

            var newParams = LoadParametersSafely();
            var newEventMap = LoadModsSafely();

            foreach (var kvp in newParams)
                parameters[kvp.Key] = kvp.Value;

            var obsolete = new List<string>();
            foreach (var existing in parameters.Keys)
            {
                if (!newParams.ContainsKey(existing))
                    obsolete.Add(existing);
            }
            foreach (var key in obsolete)
                parameters.Remove(key);

            eventMap.Clear();
            foreach (var kvp in newEventMap)
                eventMap[kvp.Key] = kvp.Value;

            Debug.Log("[ModManager] Reload complete.");
        }

        private Dictionary<string, JSONNode> LoadParametersSafely()
        {
            var result = new Dictionary<string, JSONNode>();
            string path = Path.Combine(Application.dataPath, "../Mods/Parameters/");
            if (!Directory.Exists(path)) return result;

            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                var node = JSON.Parse(json);
                if (node == null || !node.IsObject) continue;

                foreach (var kvp in node.AsObject)
                {
                    result[kvp.Key] = kvp.Value;
                    Debug.Log($"[ModManager] Loaded param: {kvp.Key} = {kvp.Value}");
                }
            }

            return result;
        }

        private Dictionary<string, List<ModDefinition>> LoadModsSafely()
        {
            var result = new Dictionary<string, List<ModDefinition>>();
            string path = Path.Combine(Application.dataPath, "../Mods/Addons/");
            if (!Directory.Exists(path)) return result;

            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                var node = JSON.Parse(json);
                if (node == null) continue;

                ModDefinition mod = new ModDefinition
                {
                    modName = node["modName"],
                    eventName = node["eventName"],
                    action = node["action"],
                    args = node["args"]
                };

                if (!result.ContainsKey(mod.eventName))
                    result[mod.eventName] = new List<ModDefinition>();

                result[mod.eventName].Add(mod);
                Debug.Log($"[ModManager] Loaded mod: {mod.modName} for event {mod.eventName}");
            }

            return result;
        }

        public void TriggerEvent(string eventName)
        {
            if (!eventMap.TryGetValue(eventName, out var mods)) return;

            foreach (var mod in mods)
            {
                ExecuteMod(mod);
            }
        }

        private void ExecuteMod(ModDefinition mod)
        {
            Debug.Log($"[ModManager] Executing mod '{mod.modName}'");

            JSONNode resolvedArgs = ResolveArgs(mod.args);
            ModAPI.TryExecute(mod.action, resolvedArgs);
        }

        private JSONNode ResolveArgs(JSONNode args)
        {
            var resolved = new JSONObject();

            foreach (KeyValuePair<string, JSONNode> kvp in args)
            {
                JSONNode value = kvp.Value;

                if (value is JSONString str && str.Value.StartsWith("@"))
                {
                    string key = str.Value.Substring(1);
                    if (parameters.TryGetValue(key, out var param))
                    {
                        resolved[kvp.Key] = param;
                        Debug.Log($"[ModManager] Resolved @{key} → {param}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ModManager] Missing parameter: @{key}");
                        resolved[kvp.Key] = value;
                    }
                }
                else
                {
                    resolved[kvp.Key] = value;
                }
            }

            return resolved;
        }

#if UNITY_EDITOR
        public bool HasLoadedMods() => eventMap.Count > 0;
        public bool HasLoadedParameters() => parameters.Count > 0;
        public Dictionary<string, List<ModDefinition>> GetLoadedMods() => eventMap;
        public Dictionary<string, JSONNode> GetLoadedParameters() => parameters;
#endif
    }
}