using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Unlocks an action so it can be triggered (useful for actions requiring explicit permission).
    /// </summary>
    public class UnlockActionAction : AddonApiAction
    {
        public override string ActionName => "UnlockAction";

        public override void Execute(JSONNode args)
        {
            if (args == null)
            {
                Debug.LogWarning("[AddonAPI] UnlockAction called without arguments.");
                return;
            }

            string actionId = args.HasKey("actionId") ? args["actionId"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[AddonAPI] UnlockAction requires an 'actionId'.");
                return;
            }

            AddonAPI.UnlockAction(actionId);
        }
    }
}
