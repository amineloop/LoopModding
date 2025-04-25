using UnityEngine;
using LoopModding.Core;

namespace LoopModding.Helpers{
    public class TriggerGameEvent : MonoBehaviour
    {
        public GameEvents[] gameEvents;
        ModManager modManager;
        [Header("Debug")]
        public bool triggerNow;

        void Update()
        {
            if(triggerNow){
                triggerNow = false;
                TriggerEvents();
            }
        }

        public void TriggerEvents(){
            if(modManager == null) modManager = ModManager.Instance;
            if(gameEvents.Length == 0) return;
            foreach(GameEvents eventToTrigger in gameEvents){
                modManager.TriggerEvent(eventToTrigger.eventName);
            }
        }
    }
}