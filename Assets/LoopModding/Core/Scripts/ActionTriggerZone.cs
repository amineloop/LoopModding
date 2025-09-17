using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.Runtime
{
    /// <summary>
    /// Calls an action when a collider enters or exits a trigger zone.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ActionTriggerZone : MonoBehaviour
    {
        [SerializeField]
        private string actionId;

        [SerializeField]
        private string requiredTag;

        [SerializeField]
        private bool triggerOnEnter = true;

        [SerializeField]
        private bool triggerOnExit;

        [SerializeField]
        [Tooltip("Optional JSON payload passed when the trigger fires.")]
        [TextArea(2, 6)]
        private string payloadJson;

        private JSONNode cachedPayload;

        private void Awake()
        {
            CachePayload();
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggerOnEnter)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            Fire();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!triggerOnExit)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            Fire();
        }

        private void Fire()
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[ActionTriggerZone] Missing actionId.");
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
                Debug.LogWarning($"[ActionTriggerZone] Failed to parse payload JSON: {ex.Message}");
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
