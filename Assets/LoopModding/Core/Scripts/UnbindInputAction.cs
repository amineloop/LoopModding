using LoopModding.Core.Runtime;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Removes a previously registered input binding.
    /// </summary>
    public class UnbindInputAction : ModApiAction
    {
        public override string ActionName => "UnbindInput";

        public override void Execute(JSONNode args)
        {
            if (args == null)
            {
                Debug.LogWarning("[MOD] UnbindInput called without arguments.");
                return;
            }

            string id = args.HasKey("id") ? args["id"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("[MOD] UnbindInput requires an 'id' to remove.");
                return;
            }

            ModInputRuntime runtime = ModInputRuntime.EnsureInstance();
            if (runtime.UnregisterBinding(id))
            {
                Debug.Log($"[MOD] UnbindInput removed binding '{id}'.");
            }
            else
            {
                Debug.LogWarning($"[MOD] UnbindInput could not find binding '{id}'.");
            }
        }
    }
}
