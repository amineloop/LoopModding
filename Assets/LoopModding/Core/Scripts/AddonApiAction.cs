using System;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Base class for AddonAPI actions. Inherit from this class to expose a new action to add-ons.
    /// </summary>
    public abstract class AddonApiAction
    {
        /// <summary>
        /// Unique identifier used by add-ons when calling this action.
        /// </summary>
        public abstract string ActionName { get; }

        /// <summary>
        /// Determines whether the action should be registered automatically when the AddonAPI initializes.
        /// </summary>
        public virtual bool ShouldAutoRegister => true;

        /// <summary>
        /// Registers this action with the AddonAPI registry.
        /// </summary>
        public void RegisterSelf()
        {
            if (string.IsNullOrWhiteSpace(ActionName))
            {
                Debug.LogWarning($"[AddonAPI] {GetType().Name} provided an empty action name and will not be registered.");
                return;
            }

            AddonAPI.Register(ActionName, Execute);
        }

        /// <summary>
        /// Executes the action using the provided arguments from the add-on call.
        /// </summary>
        /// <param name="args">Arguments passed by the add-on.</param>
        public abstract void Execute(JSONNode args);
    }
}
