using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Invokes another action by its identifier. Useful for chaining behaviours from add-ons.
    /// </summary>
    public class TriggerActionAction : AddonApiAction
    {
        public override string ActionName => "TriggerAction";

        public override void Execute(JSONNode args)
        {
            if (args == null)
            {
                Debug.LogWarning("[AddonAPI] TriggerAction called without arguments.");
                return;
            }

            string actionId = args.HasKey("actionId") ? args["actionId"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[AddonAPI] TriggerAction requires an 'actionId'.");
                return;
            }

            JSONNode payload = args.HasKey("payload") ? args["payload"] : null;
            AddonAPI.TriggerAction(actionId, payload);
        }
    }
}
