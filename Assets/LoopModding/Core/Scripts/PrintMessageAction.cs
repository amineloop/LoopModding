using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Appends a message to the in-game chat window.
    /// </summary>
    public class PrintMessageAction : AddonApiAction
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
                Debug.LogWarning("[AddonAPI] PrintMessage missing 'chatMessage' argument.");
            }
        }
    }
}
