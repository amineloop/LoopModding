using System;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Base class for ModAPI actions. Inherit from this class to expose a new action to mods.
    /// </summary>
    public abstract class ModApiAction
    {
        /// <summary>
        /// Unique identifier used by mods when calling this action.
        /// </summary>
        public abstract string ActionName { get; }

        /// <summary>
        /// Determines whether the action should be registered automatically when the ModAPI initializes.
        /// </summary>
        public virtual bool ShouldAutoRegister => true;

        /// <summary>
        /// Registers this action with the ModAPI registry.
        /// </summary>
        public void RegisterSelf()
        {
            if (string.IsNullOrWhiteSpace(ActionName))
            {
                Debug.LogWarning($"[ModAPI] {GetType().Name} provided an empty action name and will not be registered.");
                return;
            }

            ModAPI.Register(ActionName, Execute);
        }

        /// <summary>
        /// Executes the action using the provided arguments from the mod call.
        /// </summary>
        /// <param name="args">Arguments passed by the mod.</param>
        public abstract void Execute(JSONNode args);
    }
}
