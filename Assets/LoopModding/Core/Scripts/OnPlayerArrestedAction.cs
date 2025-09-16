using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Teleports the player or writes a chat message when the arrest event is triggered.
    /// </summary>
    public class OnPlayerArrestedAction : ModApiAction
    {
        public override string ActionName => "OnPlayerArrested";

        public override void Execute(JSONNode args)
        {
            if (args != null && args.HasKey("x") && args.HasKey("y") && args.HasKey("z"))
            {
                float x = args["x"].AsFloat;
                float y = args["y"].AsFloat;
                float z = args["z"].AsFloat;
                GameManager.instance.playerTransform.position = new Vector3(x, y, z);
            }
            else if (args != null && args.HasKey("chatMessage"))
            {
                string msg = args["chatMessage"];
                GameManager.instance.chatText.text += msg + "\n";
            }
            else
            {
                Debug.LogWarning("[MOD] OnPlayerArrested missing 'x/y/z' or 'chatMessage' argument.");
            }
        }
    }
}
