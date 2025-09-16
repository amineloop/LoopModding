using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using LoopModding.Core.API;

namespace LoopModding.Core
{
    public class ModManager : MonoBehaviour
    {
        public static ModManager Instance;

        private static readonly Regex placeholderRegex = new("@(?:\\{(?<braced>[A-Za-z0-9_]+)\\}|(?<key>[A-Za-z0-9_]+))");

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

                if (!eventMap.ContainsKey(mod.eventName))
                    eventMap[mod.eventName] = new List<ModDefinition>();

                eventMap[mod.eventName].Add(mod);
                Debug.Log($"[ModManager] Loaded mod: {mod.modName} for event {mod.eventName}");
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
            if (args == null || args.Tag == JSONNodeType.None || args.Tag == JSONNodeType.NullValue)
                return new JSONObject();

            return ResolveNode(args, new HashSet<string>());
        }

        private JSONNode ResolveNode(JSONNode node, HashSet<string> visitedKeys)
        {
            if (node == null)
                return JSONNull.CreateOrGet();

            switch (node.Tag)
            {
                case JSONNodeType.Object:
                    var obj = new JSONObject();
                    foreach (KeyValuePair<string, JSONNode> kvp in node.AsObject)
                        obj[kvp.Key] = ResolveNode(kvp.Value, visitedKeys);
                    return obj;
                case JSONNodeType.Array:
                    var array = new JSONArray();
                    foreach (JSONNode child in node.AsArray)
                        array.Add(ResolveNode(child, visitedKeys));
                    return array;
                case JSONNodeType.String:
                    return ResolveStringNode(node.Value, visitedKeys);
                case JSONNodeType.None:
                    return JSONNull.CreateOrGet();
                default:
                    return node.Clone();
            }
        }

        private JSONNode ResolveStringNode(string value, HashSet<string> visitedKeys)
        {
            if (string.IsNullOrEmpty(value))
                return new JSONString(string.Empty);

            if (TryGetExactPlaceholder(value, out string key))
            {
                if (visitedKeys.Contains(key))
                {
                    Debug.LogWarning($"[ModManager] Detected circular parameter reference: @{key}");
                    return new JSONString(value);
                }

                visitedKeys.Add(key);

                try
                {
                    if (parameters.TryGetValue(key, out var param))
                    {
                        JSONNode resolved = ResolveNode(param, visitedKeys);
                        Debug.Log($"[ModManager] Resolved @{key} → {resolved}");
                        return resolved;
                    }

                    Debug.LogWarning($"[ModManager] Missing parameter: @{key}");
                    return new JSONString(value);
                }
                finally
                {
                    visitedKeys.Remove(key);
                }
            }

            if (!value.Contains("@"))
                return new JSONString(value);

            string replaced = placeholderRegex.Replace(value, match =>
            {
                string placeholderKey = match.Groups["braced"].Success
                    ? match.Groups["braced"].Value
                    : match.Groups["key"].Value;
                if (!IsValidPlaceholderKey(placeholderKey))
                    return match.Value;
                if (visitedKeys.Contains(placeholderKey))
                {
                    Debug.LogWarning($"[ModManager] Detected circular parameter reference: @{placeholderKey}");
                    return match.Value;
                }

                visitedKeys.Add(placeholderKey);

                try
                {
                    if (parameters.TryGetValue(placeholderKey, out var param))
                    {
                        JSONNode resolved = ResolveNode(param, visitedKeys);
                        Debug.Log($"[ModManager] Resolved @{placeholderKey} → {resolved}");
                        return GetNodeStringValue(resolved);
                    }

                    Debug.LogWarning($"[ModManager] Missing parameter: @{placeholderKey}");
                    return match.Value;
                }
                finally
                {
                    visitedKeys.Remove(placeholderKey);
                }
            });

            return new JSONString(replaced);
        }

        private static bool TryGetExactPlaceholder(string value, out string key)
        {
            key = string.Empty;

            if (string.IsNullOrEmpty(value) || value[0] != '@')
                return false;

            if (value.Length > 2 && value[1] == '{' && value[^1] == '}')
            {
                string candidate = value.Substring(2, value.Length - 3);
                if (IsValidPlaceholderKey(candidate))
                {
                    key = candidate;
                    return true;
                }

                return false;
            }

            string fallback = value.Substring(1);
            if (IsValidPlaceholderKey(fallback))
            {
                key = fallback;
                return true;
            }

            return false;
        }

        private static bool IsValidPlaceholderKey(string candidate)
        {
            if (string.IsNullOrEmpty(candidate))
                return false;

            foreach (char c in candidate)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    return false;
            }

            return true;
        }

        private string GetNodeStringValue(JSONNode node)
        {
            if (node == null)
                return string.Empty;

            if (node.Tag == JSONNodeType.Array || node.Tag == JSONNodeType.Object)
                return node.ToString();

            return node.Value;
        }

#if UNITY_EDITOR
        public bool HasLoadedMods() => eventMap.Count > 0;
        public bool HasLoadedParameters() => parameters.Count > 0;
        public Dictionary<string, List<ModDefinition>> GetLoadedMods() => eventMap;
        public Dictionary<string, JSONNode> GetLoadedParameters() => parameters;
#endif
    }
}