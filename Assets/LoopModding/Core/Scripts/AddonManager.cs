using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using LoopModding.Core.API;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core
{
    /// <summary>
    /// Loads add-on definitions and exposes the runtime event bus that add-ons consume.
    /// </summary>
    public class AddonManager : MonoBehaviour
    {
        private const string ParametersFolder = "../Mods/Parameters/";
        private const string AddonsFolder = "../Mods/Addons/";

        public static AddonManager Instance { get; private set; }

        private static readonly Regex placeholderRegex = new("@(?:\\{(?<braced>[A-Za-z0-9_]+)\\}|(?<key>[A-Za-z0-9_]+))");

        private readonly Dictionary<string, List<AddonDefinition>> eventMap = new();
        private readonly Dictionary<string, JSONNode> parameters = new();

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
            LoadAllAddons();
            ActionManager.EnsureInstance();
        }

        /// <summary>
        /// Returns true if at least one add-on listens to the provided event name.
        /// </summary>
        public bool HasEvent(string eventName) => eventMap.ContainsKey(eventName);

        /// <summary>
        /// Reloads parameters and add-on definitions from disk.
        /// </summary>
        public void ReloadFolders()
        {
            Debug.Log("[AddonManager] Reloading add-ons, parameters and actions...");

            var newParams = LoadParametersSnapshot();
            var newEventMap = LoadAddonsSnapshot();

            foreach (var kvp in newParams)
            {
                parameters[kvp.Key] = kvp.Value;
            }

            var obsolete = new List<string>();
            foreach (var key in parameters.Keys)
            {
                if (!newParams.ContainsKey(key))
                {
                    obsolete.Add(key);
                }
            }

            foreach (var key in obsolete)
            {
                parameters.Remove(key);
            }

            eventMap.Clear();
            foreach (var kvp in newEventMap)
            {
                eventMap[kvp.Key] = kvp.Value;
            }

            ActionManager.EnsureInstance().ReloadDefinitions();
            Debug.Log("[AddonManager] Reload complete.");
        }

        /// <summary>
        /// Triggers an event consumed by add-ons.
        /// </summary>
        public void TriggerEvent(string eventName, JSONNode runtimeArgs = null)
        {
            if (!eventMap.TryGetValue(eventName, out var addons))
            {
                return;
            }

            foreach (var addon in addons)
            {
                ExecuteAddon(addon, runtimeArgs);
            }
        }

        private void ExecuteAddon(AddonDefinition addon, JSONNode runtimeArgs)
        {
            Debug.Log($"[AddonManager] Executing add-on '{addon.addonName}' for event '{addon.eventName}'.");

            JSONNode resolvedArgs = ResolveArgs(addon.args, runtimeArgs);
            AddonAPI.TryExecute(addon.action, resolvedArgs);
        }

        private void LoadAllParameters()
        {
            string path = Path.Combine(Application.dataPath, ParametersFolder);
            if (!Directory.Exists(path))
            {
                Debug.LogWarning("[AddonManager] No parameters folder found.");
                return;
            }

            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                var node = JSON.Parse(json);

                if (node == null || !node.IsObject)
                {
                    continue;
                }

                foreach (var kvp in node.AsObject)
                {
                    parameters[kvp.Key] = kvp.Value;
                    Debug.Log($"[AddonManager] Loaded param: {kvp.Key} = {kvp.Value}");
                }
            }
        }

        private void LoadAllAddons()
        {
            string path = Path.Combine(Application.dataPath, AddonsFolder);
            if (!Directory.Exists(path))
            {
                Debug.LogWarning("[AddonManager] No Mods/Addons folder found.");
                return;
            }

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                var node = JSON.Parse(json);
                if (!TryParseAddon(node, out var addon))
                {
                    continue;
                }

                if (!eventMap.ContainsKey(addon.eventName))
                {
                    eventMap[addon.eventName] = new List<AddonDefinition>();
                }

                eventMap[addon.eventName].Add(addon);
                Debug.Log($"[AddonManager] Loaded add-on: {addon.addonName} for event {addon.eventName}");
            }
        }

        private Dictionary<string, JSONNode> LoadParametersSnapshot()
        {
            var result = new Dictionary<string, JSONNode>();
            string path = Path.Combine(Application.dataPath, ParametersFolder);
            if (!Directory.Exists(path))
            {
                return result;
            }

            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                var node = JSON.Parse(json);
                if (node == null || !node.IsObject)
                {
                    continue;
                }

                foreach (var kvp in node.AsObject)
                {
                    result[kvp.Key] = kvp.Value;
                    Debug.Log($"[AddonManager] Loaded param: {kvp.Key} = {kvp.Value}");
                }
            }

            return result;
        }

        private Dictionary<string, List<AddonDefinition>> LoadAddonsSnapshot()
        {
            var result = new Dictionary<string, List<AddonDefinition>>();
            string path = Path.Combine(Application.dataPath, AddonsFolder);
            if (!Directory.Exists(path))
            {
                return result;
            }

            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                var node = JSON.Parse(json);
                if (!TryParseAddon(node, out var addon))
                {
                    continue;
                }

                if (!result.ContainsKey(addon.eventName))
                {
                    result[addon.eventName] = new List<AddonDefinition>();
                }

                result[addon.eventName].Add(addon);
                Debug.Log($"[AddonManager] Loaded add-on: {addon.addonName} for event {addon.eventName}");
            }

            return result;
        }

        private bool TryParseAddon(JSONNode node, out AddonDefinition addon)
        {
            addon = null;
            if (node == null)
            {
                return false;
            }

            string addonName = ReadString(node, "addonName");
            if (string.IsNullOrWhiteSpace(addonName))
            {
                addonName = ReadString(node, "modName");
            }

            string eventName = ReadString(node, "eventName");
            string action = ReadString(node, "action");

            if (string.IsNullOrWhiteSpace(addonName) || string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(action))
            {
                Debug.LogWarning("[AddonManager] Invalid add-on definition detected. Ensure 'addonName', 'eventName' and 'action' are present.");
                return false;
            }

            JSONNode args = node.HasKey("args") ? node["args"].Clone() : new JSONObject();

            addon = new AddonDefinition
            {
                addonName = addonName,
                eventName = eventName,
                action = action,
                args = args
            };

            return true;
        }

        private static string ReadString(JSONNode node, string key)
        {
            return node != null && node.HasKey(key) ? node[key].Value : string.Empty;
        }

        private JSONNode ResolveArgs(JSONNode definitionArgs, JSONNode runtimeArgs)
        {
            JSONNode merged = MergeArgs(definitionArgs, runtimeArgs);
            if (merged == null || merged.Tag == JSONNodeType.None || merged.Tag == JSONNodeType.NullValue)
            {
                return new JSONObject();
            }

            return ResolveNode(merged, new HashSet<string>());
        }

        private JSONNode MergeArgs(JSONNode definitionArgs, JSONNode runtimeArgs)
        {
            bool hasDefinition = definitionArgs != null && definitionArgs.Tag != JSONNodeType.None && definitionArgs.Tag != JSONNodeType.NullValue;
            bool hasRuntime = runtimeArgs != null && runtimeArgs.Tag != JSONNodeType.None && runtimeArgs.Tag != JSONNodeType.NullValue;

            if (!hasDefinition && !hasRuntime)
            {
                return new JSONObject();
            }

            if (!hasRuntime)
            {
                return definitionArgs.Clone();
            }

            if (!hasDefinition)
            {
                return runtimeArgs.Clone();
            }

            if (definitionArgs.Tag == JSONNodeType.Object && runtimeArgs.Tag == JSONNodeType.Object)
            {
                var result = new JSONObject();
                foreach (var kvp in definitionArgs.AsObject)
                {
                    result[kvp.Key] = kvp.Value.Clone();
                }

                foreach (var kvp in runtimeArgs.AsObject)
                {
                    result[kvp.Key] = kvp.Value.Clone();
                }

                return result;
            }

            // Fallback: runtime arguments override the definition entirely when the structures differ.
            return runtimeArgs.Clone();
        }

        private JSONNode ResolveNode(JSONNode node, HashSet<string> visitedKeys)
        {
            if (node == null)
            {
                return JSONNull.CreateOrGet();
            }

            switch (node.Tag)
            {
                case JSONNodeType.Object:
                    var obj = new JSONObject();
                    foreach (KeyValuePair<string, JSONNode> kvp in node.AsObject)
                    {
                        obj[kvp.Key] = ResolveNode(kvp.Value, visitedKeys);
                    }

                    return obj;
                case JSONNodeType.Array:
                    var array = new JSONArray();
                    foreach (JSONNode child in node.AsArray)
                    {
                        array.Add(ResolveNode(child, visitedKeys));
                    }

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
            {
                return new JSONString(string.Empty);
            }

            if (TryGetExactPlaceholder(value, out string key))
            {
                if (visitedKeys.Contains(key))
                {
                    Debug.LogWarning($"[AddonManager] Detected circular parameter reference: @{key}");
                    return new JSONString(value);
                }

                visitedKeys.Add(key);

                try
                {
                    if (parameters.TryGetValue(key, out var param))
                    {
                        JSONNode resolved = ResolveNode(param, visitedKeys);
                        Debug.Log($"[AddonManager] Resolved @{key} → {resolved}");
                        return resolved;
                    }

                    Debug.LogWarning($"[AddonManager] Missing parameter: @{key}");
                    return new JSONString(value);
                }
                finally
                {
                    visitedKeys.Remove(key);
                }
            }

            if (!value.Contains("@"))
            {
                return new JSONString(value);
            }

            string replaced = placeholderRegex.Replace(value, match =>
            {
                string placeholderKey = match.Groups["braced"].Success
                    ? match.Groups["braced"].Value
                    : match.Groups["key"].Value;

                if (!IsValidPlaceholderKey(placeholderKey))
                {
                    return match.Value;
                }

                if (visitedKeys.Contains(placeholderKey))
                {
                    Debug.LogWarning($"[AddonManager] Detected circular parameter reference: @{placeholderKey}");
                    return match.Value;
                }

                visitedKeys.Add(placeholderKey);

                try
                {
                    if (parameters.TryGetValue(placeholderKey, out var param))
                    {
                        JSONNode resolved = ResolveNode(param, visitedKeys);
                        Debug.Log($"[AddonManager] Resolved @{placeholderKey} → {resolved}");
                        return GetNodeStringValue(resolved);
                    }

                    Debug.LogWarning($"[AddonManager] Missing parameter: @{placeholderKey}");
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
            {
                return false;
            }

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
            {
                return false;
            }

            foreach (char c in candidate)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        private string GetNodeStringValue(JSONNode node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            if (node.Tag == JSONNodeType.Array || node.Tag == JSONNodeType.Object)
            {
                return node.ToString();
            }

            return node.Value;
        }

#if UNITY_EDITOR
        public bool HasLoadedAddons() => eventMap.Count > 0;
        public bool HasLoadedParameters() => parameters.Count > 0;
        public Dictionary<string, List<AddonDefinition>> GetLoadedAddons() => eventMap;
        public Dictionary<string, JSONNode> GetLoadedParameters() => parameters;
#endif
    }
}
