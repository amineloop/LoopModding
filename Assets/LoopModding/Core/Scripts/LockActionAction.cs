using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Locks a previously unlocked action.
    /// </summary>
    public class LockActionAction : AddonApiAction
    {
        public override string ActionName => "LockAction";

        public override void Execute(JSONNode args)
        {
            if (args == null)
            {
                Debug.LogWarning("[AddonAPI] LockAction called without arguments.");
                return;
            }

            string actionId = args.HasKey("actionId") ? args["actionId"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[AddonAPI] LockAction requires an 'actionId'.");
                return;
            }

            AddonAPI.LockAction(actionId);
        }
    }
}
