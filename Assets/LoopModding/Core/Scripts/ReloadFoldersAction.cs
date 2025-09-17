using LoopModding.Core;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Reloads the mod and parameter folders from disk.
    /// </summary>
    public class ReloadFoldersAction : AddonApiAction
    {
        public override string ActionName => "ReloadFolders";

        public override void Execute(JSONNode args)
        {
            AddonManager.Instance.ReloadFolders();
            Debug.Log("[AddonAPI] Reloaded folders.");
        }
    }
}
