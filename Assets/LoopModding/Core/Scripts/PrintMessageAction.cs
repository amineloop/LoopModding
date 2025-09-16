using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Appends a message to the in-game chat window.
    /// </summary>
    public class PrintMessageAction : ModApiAction
    {
        public override string ActionName => "PrintMessage";

        public override void Execute(JSONNode args)
        {
            if (args != null && args.HasKey("chatMessage"))
            {
                string msg = args["chatMessage"];
                GameManager.instance.chatText.text += msg + "\n";
            }
            else
            {
                Debug.LogWarning("[MOD] PrintMessage missing 'chatMessage' argument.");
            }
        }
    }
}
