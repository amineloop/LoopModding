using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API{

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
            // Register default actions here
            Register("ReloadFolders", args =>
            {
                ModManager.Instance.ReloadFolders();
                Debug.Log("[MOD] Reloaded folders.");
            });

            Register("PrintMessage", args =>
            {
                if (args.HasKey("chatMessage"))
                {
                    string msg = args["chatMessage"];
                    GameManager.instance.chatText.text += msg + "\n";
                }
                else
                {
                    Debug.LogWarning("[MOD] PrintMessage missing 'chatMessage' argument.");
                }
            });

            Register("OnPlayerArrested", args =>
            {
                if (args.HasKey("x") && args.HasKey("y") && args.HasKey("z"))
                {
                    float x = args["x"].AsFloat;
                    float y = args["y"].AsFloat;
                    float z = args["z"].AsFloat;
                    GameManager.instance.playerTransform.position = new Vector3(x, y, z);
                }else if(args.HasKey("chatMessage"))
                {
                    string msg = args["chatMessage"];
                    GameManager.instance.chatText.text += msg + "\n";
                }
                else
                {
                    Debug.LogWarning("[MOD] OnPlayerArrested missing 'x/y/z' or 'chatMessage' argument.");
                }
            });

            Register("TeleportPlayer", args =>
            {
                if (args.HasKey("x") && args.HasKey("y") && args.HasKey("z"))
                {
                    float x = args["x"].AsFloat;
                    float y = args["y"].AsFloat;
                    float z = args["z"].AsFloat;
                    GameManager.instance.playerTransform.position = new Vector3(x, y, z);
                    // PlayerController.Instance.TeleportTo(x, y, z); // Optional
                }
                else
                {
                    Debug.LogWarning("[MOD] TeleportPlayer missing x/y/z values.");
                }
            });

            // Add more default actions here, like "TakeMoney", "PlaySound", etc.
        }

        /// <summary>
        /// Registers a new mod action by name.
        /// </summary>
        public static void Register(string actionName, Action<JSONNode> callback)
        {
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

    }

}