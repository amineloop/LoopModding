using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{

    /// <summary>
    /// Public static API used by mods to interact with the game.
    /// Handles mod-triggered actions via a centralized registry.
    /// </summary>
    public static class ModAPI
    {
        private static readonly Dictionary<string, Action<JSONNode>> registry = new();

        // Called once when class is first accessed
        static ModAPI()
        {
            AutoRegisterActions();
        }

        /// <summary>
        /// Registers a new mod action by name.
        /// </summary>
        public static void Register(string actionName, Action<JSONNode> callback)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                Debug.LogWarning("[ModAPI] Attempted to register an action with an empty name.");
                return;
            }

            if (callback == null)
            {
                Debug.LogWarning($"[ModAPI] Attempted to register action '{actionName}' with a null callback.");
                return;
            }

            if (!registry.ContainsKey(actionName))
            {
                registry[actionName] = callback;
                Debug.Log($"[ModAPI] Registered action: {actionName}");
            }
            else
            {
                Debug.LogWarning($"[ModAPI] Action '{actionName}' is already registered.");
            }
        }

        /// <summary>
        /// Tries to execute a mod action if it exists.
        /// </summary>
        public static void TryExecute(string actionName, JSONNode args)
        {
            // ✅ 1. Exécute l'action principale
            if (registry.TryGetValue(actionName, out var action))
            {
                action.Invoke(args);
            }
            else
            {
                Debug.LogWarning($"[ModAPI] Unknown action '{actionName}'");
            }

            // ✅ 2. Traitement automatique des paramètres partagés
            HandleCommonArgs(args);
        }

        /// <summary>
        /// Handles global/mod-wide arguments that should trigger common effects (e.g. chatMessage).
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

            // You could add more shared triggers here, like:
            // - playSound
            // - showNotification
            // - screenShake
            // - delay or repeat, etc.
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
            var actionBaseType = typeof(ModApiAction);
            var actionTypes = actionBaseType.Assembly.GetTypes()
                .Where(t => actionBaseType.IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in actionTypes)
            {
                try
                {
                    if (Activator.CreateInstance(type) is ModApiAction actionInstance)
                    {
                        if (actionInstance.ShouldAutoRegister)
                        {
                            actionInstance.RegisterSelf();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ModAPI] Type '{type.FullName}' could not be instantiated as a ModApiAction.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModAPI] Failed to auto-register action '{type.FullName}': {ex}");
                }
            }
        }

    }

}
