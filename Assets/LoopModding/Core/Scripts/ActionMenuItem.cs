using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.Runtime
{
    /// <summary>
    /// Lightweight bridge that can be hooked from UI events (e.g. dropdowns, menus) to trigger an action.
    /// </summary>
    public class ActionMenuItem : MonoBehaviour
    {
        [SerializeField]
        private string actionId;

        [SerializeField]
        [Tooltip("Optional JSON payload passed when the menu item is activated.")]
        [TextArea(2, 6)]
        private string payloadJson;

        private JSONNode cachedPayload;

        private void Awake()
        {
            CachePayload();
        }

        /// <summary>
        /// Invoked via UnityEvents to trigger the configured action.
        /// </summary>
        public void TriggerAction()
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[ActionMenuItem] Missing actionId.");
                return;
            }

            JSONNode payloadClone = cachedPayload != null ? cachedPayload.Clone() : null;
            ActionManager.EnsureInstance().TriggerAction(actionId, payloadClone);
        }

        public void SetActionId(string newActionId)
        {
            actionId = newActionId;
        }

        public void SetPayload(JSONNode payload)
        {
            cachedPayload = payload;
        }

        private void CachePayload()
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                cachedPayload = null;
                return;
            }

            try
            {
                cachedPayload = JSON.Parse(payloadJson);
            }
            catch (System.Exception ex)
            {
                cachedPayload = null;
                Debug.LogWarning($"[ActionMenuItem] Failed to parse payload JSON: {ex.Message}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CachePayload();
        }
#endif
    }
}
