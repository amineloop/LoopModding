using System;
using System.Collections.Generic;
using System.Linq;
using LoopModding.Core;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Public static API used by add-ons to interact with the game.
    /// Handles add-on triggered actions via a centralized registry and exposes shortcuts to the ActionManager.
    /// </summary>
    public static class AddonAPI
    {
        private static readonly Dictionary<string, Action<JSONNode>> registry = new();

        static AddonAPI()
        {
            AutoRegisterActions();
        }

        /// <summary>
        /// Registers a new add-on action by name.
        /// </summary>
        public static void Register(string actionName, Action<JSONNode> callback)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                Debug.LogWarning("[AddonAPI] Attempted to register an action with an empty name.");
                return;
            }

            if (callback == null)
            {
                Debug.LogWarning($"[AddonAPI] Attempted to register action '{actionName}' with a null callback.");
                return;
            }

            if (!registry.ContainsKey(actionName))
            {
                registry[actionName] = callback;
                Debug.Log($"[AddonAPI] Registered action: {actionName}");
            }
            else
            {
                Debug.LogWarning($"[AddonAPI] Action '{actionName}' is already registered.");
            }
        }

        /// <summary>
        /// Tries to execute an add-on action if it exists.
        /// </summary>
        public static void TryExecute(string actionName, JSONNode args)
        {
            if (registry.TryGetValue(actionName, out var action))
            {
                action.Invoke(args);
            }
            else
            {
                Debug.LogWarning($"[AddonAPI] Unknown action '{actionName}'");
            }

            HandleCommonArgs(args);
        }

        /// <summary>
        /// Exposes the ActionManager entry point to add-ons.
        /// </summary>
        public static void TriggerAction(string actionId, JSONNode payload = null)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[AddonAPI] TriggerAction called with an empty id.");
                return;
            }

            ActionManager.EnsureInstance().TriggerAction(actionId, payload);
        }

        /// <summary>
        /// Unlocks an action in the ActionManager, allowing protected actions to be triggered.
        /// </summary>
        public static void UnlockAction(string actionId)
        {
            ActionManager.EnsureInstance().UnlockAction(actionId);
        }

        /// <summary>
        /// Locks an action again (useful for temporary permissions).
        /// </summary>
        public static void LockAction(string actionId)
        {
            ActionManager.EnsureInstance().LockAction(actionId);
        }

        /// <summary>
        /// Handles global arguments that should trigger shared effects (e.g. chatMessage).
        /// </summary>
        private static void HandleCommonArgs(JSONNode args)
        {
            if (args == null)
            {
                return;
            }

            if (args.HasKey("chatMessage"))
            {
                string msg = args["chatMessage"];
                GameManager.instance.chatText.text += msg + "\n";
            }
        }

        public static List<string> GetRegisteredActions()
        {
            return registry.Keys.ToList();
        }

        public static bool HasRegisteredActions()
        {
            return registry.Count > 0;
        }

        private static void AutoRegisterActions()
        {
            var actionBaseType = typeof(AddonApiAction);
            var actionTypes = actionBaseType.Assembly.GetTypes()
                .Where(t => actionBaseType.IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in actionTypes)
            {
                try
                {
                    if (Activator.CreateInstance(type) is AddonApiAction actionInstance)
                    {
                        if (actionInstance.ShouldAutoRegister)
                        {
                            actionInstance.RegisterSelf();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AddonAPI] Type '{type.FullName}' could not be instantiated as an AddonApiAction.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AddonAPI] Failed to auto-register action '{type.FullName}': {ex}");
                }
            }
        }
    }
}
