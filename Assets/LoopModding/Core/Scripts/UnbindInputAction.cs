using LoopModding.Core.Runtime;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Removes a previously registered input binding.
    /// </summary>
    public class UnbindInputAction : AddonApiAction
    {
        public override string ActionName => "UnbindInput";

        public override void Execute(JSONNode args)
        {
            if (args == null)
            {
                Debug.LogWarning("[AddonAPI] UnbindInput called without arguments.");
                return;
            }

            string id = args.HasKey("id") ? args["id"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("[AddonAPI] UnbindInput requires an 'id' to remove.");
                return;
            }

            ActionInputBridge runtime = ActionInputBridge.EnsureInstance();
            if (runtime.UnregisterBinding(id))
            {
                Debug.Log($"[AddonAPI] UnbindInput removed binding '{id}'.");
            }
            else
            {
                Debug.LogWarning($"[AddonAPI] UnbindInput could not find binding '{id}'.");
            }
        }
    }
}
