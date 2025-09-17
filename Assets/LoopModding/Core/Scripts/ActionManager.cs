using System;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core
{
    /// <summary>
    /// Central registry that maps action identifiers to one or many add-on events.
    /// </summary>
    public class ActionManager : MonoBehaviour
    {
        private const string ActionsFolder = "../Mods/Actions/";
        private const string RuntimeName = "ActionManager";

        public static ActionManager Instance { get; private set; }

        private readonly Dictionary<string, ActionDefinition> definitions = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> cooldownState = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> unlockedActions = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<PendingAction> pendingActions = new();
        private readonly HashSet<string> executionStack = new(StringComparer.OrdinalIgnoreCase);

        private bool initialized;
        private bool isProcessingQueue;

        private struct PendingAction
        {
            public ActionDefinition Definition;
            public JSONNode Payload;
        }

        public static ActionManager EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            ActionManager existing = FindObjectOfType<ActionManager>();
            if (existing != null)
            {
                Instance = existing;
                Instance.Initialize();
                return Instance;
            }

            GameObject go = new(RuntimeName);
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ActionManager>();
            Instance.Initialize();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            LoadDefinitions();
            initialized = true;
        }

        /// <summary>
        /// Reloads action definitions from disk.
        /// </summary>
        public void ReloadDefinitions()
        {
            LoadDefinitions();
        }

        public bool HasAction(string actionId) => definitions.ContainsKey(actionId);

        public void UnlockAction(string actionId)
        {
            if (!string.IsNullOrWhiteSpace(actionId))
            {
                unlockedActions.Add(actionId);
            }
        }

        public void LockAction(string actionId)
        {
            if (!string.IsNullOrWhiteSpace(actionId))
            {
                unlockedActions.Remove(actionId);
            }
        }

        /// <summary>
        /// Triggers an action by id.
        /// </summary>
        public void TriggerAction(string actionId, JSONNode payload = null)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[ActionManager] TriggerAction called with an empty id.");
                return;
            }

            if (!definitions.TryGetValue(actionId, out var definition))
            {
                Debug.LogWarning($"[ActionManager] Unknown action '{actionId}'.");
                return;
            }

            if (!definition.Enabled)
            {
                Debug.LogWarning($"[ActionManager] Action '{actionId}' is disabled.");
                return;
            }

            if (definition.RequiresUnlock && !unlockedActions.Contains(actionId))
            {
                Debug.LogWarning($"[ActionManager] Action '{actionId}' requires unlock before it can be used.");
                return;
            }

            if (IsOnCooldown(definition))
            {
                Debug.LogWarning($"[ActionManager] Action '{actionId}' is on cooldown.");
                return;
            }

            JSONNode mergedPayload = MergePayload(definition.DefaultPayload, payload);
            pendingActions.Add(new PendingAction
            {
                Definition = definition,
                Payload = mergedPayload
            });

            ProcessQueue();
        }

        private void ProcessQueue()
        {
            if (isProcessingQueue)
            {
                return;
            }

            isProcessingQueue = true;

            try
            {
                while (pendingActions.Count > 0)
                {
                    pendingActions.Sort((a, b) => b.Definition.Priority.CompareTo(a.Definition.Priority));
                    var pending = pendingActions[0];
                    pendingActions.RemoveAt(0);
                    ExecuteDefinition(pending.Definition, pending.Payload);
                }
            }
            finally
            {
                isProcessingQueue = false;
            }
        }

        private void ExecuteDefinition(ActionDefinition definition, JSONNode payload)
        {
            if (definition == null)
            {
                return;
            }

            if (executionStack.Contains(definition.ActionId))
            {
                Debug.LogWarning($"[ActionManager] Recursive trigger detected for action '{definition.ActionId}'.");
                return;
            }

            executionStack.Add(definition.ActionId);

            try
            {
                foreach (string eventName in definition.EventNames)
                {
                    AddonManager.Instance?.TriggerEvent(eventName, payload);
                }

                if (definition.Cooldown > 0f)
                {
                    cooldownState[definition.ActionId] = Time.time + definition.Cooldown;
                }
            }
            finally
            {
                executionStack.Remove(definition.ActionId);
            }
        }

        private bool IsOnCooldown(ActionDefinition definition)
        {
            if (definition.Cooldown <= 0f)
            {
                return false;
            }

            if (!cooldownState.TryGetValue(definition.ActionId, out float availableAt))
            {
                return false;
            }

            return Time.time < availableAt;
        }

        private JSONNode MergePayload(JSONNode basePayload, JSONNode overridePayload)
        {
            bool hasBase = basePayload != null && basePayload.Tag != JSONNodeType.None && basePayload.Tag != JSONNodeType.NullValue;
            bool hasOverride = overridePayload != null && overridePayload.Tag != JSONNodeType.None && overridePayload.Tag != JSONNodeType.NullValue;

            if (!hasBase && !hasOverride)
            {
                return new JSONObject();
            }

            if (!hasOverride)
            {
                return basePayload.Clone();
            }

            if (!hasBase)
            {
                return overridePayload.Clone();
            }

            if (basePayload.Tag == JSONNodeType.Object && overridePayload.Tag == JSONNodeType.Object)
            {
                var result = new JSONObject();

                foreach (var kvp in basePayload.AsObject)
                {
                    result[kvp.Key] = kvp.Value.Clone();
                }

                foreach (var kvp in overridePayload.AsObject)
                {
                    result[kvp.Key] = kvp.Value.Clone();
                }

                return result;
            }

            return overridePayload.Clone();
        }

        private void LoadDefinitions()
        {
            definitions.Clear();
            cooldownState.Clear();
            pendingActions.Clear();
            executionStack.Clear();

            string path = Path.Combine(Application.dataPath, ActionsFolder);
            if (!Directory.Exists(path))
            {
                Debug.LogWarning("[ActionManager] No Mods/Actions folder found.");
                return;
            }

            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                var node = JSON.Parse(json);
                if (!TryParseAction(node, out ActionDefinition definition))
                {
                    continue;
                }

                if (definitions.ContainsKey(definition.ActionId))
                {
                    Debug.LogWarning($"[ActionManager] Duplicate action id '{definition.ActionId}' ignored.");
                    continue;
                }

                definitions[definition.ActionId] = definition;
                Debug.Log($"[ActionManager] Loaded action '{definition.ActionId}' targeting {definition.EventNames.Count} event(s).");
            }

            // Remove unlocks for actions that no longer exist.
            unlockedActions.RemoveWhere(actionId => !definitions.ContainsKey(actionId));
        }

        private bool TryParseAction(JSONNode node, out ActionDefinition definition)
        {
            definition = null;
            if (node == null)
            {
                return false;
            }

            string actionId = node.HasKey("actionId") ? node["actionId"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[ActionManager] Action definition missing 'actionId'.");
                return false;
            }

            var events = ExtractEvents(node);
            if (events.Count == 0)
            {
                Debug.LogWarning($"[ActionManager] Action '{actionId}' does not specify any target events.");
                return false;
            }

            definition = new ActionDefinition
            {
                ActionId = actionId,
                EventNames = events,
                Description = ReadString(node, "description"),
                Category = ReadString(node, "category"),
                Icon = ReadString(node, "icon"),
                Enabled = !node.HasKey("enabled") || node["enabled"].AsBool,
                Priority = node.HasKey("priority") ? node["priority"].AsInt : 0,
                Cooldown = node.HasKey("cooldown") ? Mathf.Max(0f, node["cooldown"].AsFloat) : 0f,
                RequiresUnlock = node.HasKey("requiresUnlock") && node["requiresUnlock"].AsBool,
                DefaultPayload = node.HasKey("parameters") ? node["parameters"].Clone() : (node.HasKey("args") ? node["args"].Clone() : new JSONObject())
            };

            return true;
        }

        private static List<string> ExtractEvents(JSONNode node)
        {
            var events = new List<string>();

            void TryAdd(string value)
            {
                if (!string.IsNullOrWhiteSpace(value) && !events.Contains(value))
                {
                    events.Add(value);
                }
            }

            if (node.HasKey("event"))
            {
                TryAdd(node["event"].Value);
            }

            if (node.HasKey("eventName"))
            {
                TryAdd(node["eventName"].Value);
            }

            if (node.HasKey("events"))
            {
                JSONNode eventsNode = node["events"];
                if (eventsNode.Tag == JSONNodeType.Array)
                {
                    foreach (JSONNode child in eventsNode.AsArray)
                    {
                        TryAdd(child.Value);
                    }
                }
                else if (eventsNode.Tag == JSONNodeType.String)
                {
                    TryAdd(eventsNode.Value);
                }
            }

            return events;
        }

        private static string ReadString(JSONNode node, string key)
        {
            return node != null && node.HasKey(key) ? node[key].Value : string.Empty;
        }
    }
}
