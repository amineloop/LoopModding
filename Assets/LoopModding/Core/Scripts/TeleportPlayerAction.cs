using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Teleports the player to the provided coordinates.
    /// </summary>
    public class TeleportPlayerAction : ModApiAction
    {
        public override string ActionName => "TeleportPlayer";

        public override void Execute(JSONNode args)
        {
            if (args != null && args.HasKey("x") && args.HasKey("y") && args.HasKey("z"))
            {
                float x = args["x"].AsFloat;
                float y = args["y"].AsFloat;
                float z = args["z"].AsFloat;
                GameManager.instance.playerTransform.position = new Vector3(x, y, z);
                // PlayerController.Instance.TeleportTo(x, y, z); // Optional hook for future player controller integration.
            }
            else
            {
                Debug.LogWarning("[MOD] TeleportPlayer missing x/y/z values.");
            }
        }
    }
}
