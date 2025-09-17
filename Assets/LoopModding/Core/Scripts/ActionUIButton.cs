using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace LoopModding.Core.Runtime
{
    /// <summary>
    /// Bridges a standard Unity UI Button to an action defined in the ActionManager.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class ActionUIButton : MonoBehaviour
    {
        [SerializeField]
        private string actionId;

        [SerializeField]
        [Tooltip("Optional JSON payload passed to the action when triggered.")]
        [TextArea(2, 6)]
        private string payloadJson;

        private Button cachedButton;
        private JSONNode cachedPayload;

        private void Awake()
        {
            cachedButton = GetComponent<Button>();
            CachePayload();
            cachedButton.onClick.AddListener(TriggerAction);
        }

        private void OnDestroy()
        {
            if (cachedButton != null)
            {
                cachedButton.onClick.RemoveListener(TriggerAction);
            }
        }

        /// <summary>
        /// Allows other scripts to change the action id at runtime.
        /// </summary>
        public void SetActionId(string newActionId)
        {
            actionId = newActionId;
        }

        /// <summary>
        /// Triggers the configured action. Can be called manually from UnityEvents.
        /// </summary>
        public void TriggerAction()
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[ActionUIButton] Missing actionId.");
                return;
            }

            JSONNode payloadClone = cachedPayload != null ? cachedPayload.Clone() : null;
            ActionManager.EnsureInstance().TriggerAction(actionId, payloadClone);
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
                Debug.LogWarning($"[ActionUIButton] Failed to parse payload JSON: {ex.Message}");
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
