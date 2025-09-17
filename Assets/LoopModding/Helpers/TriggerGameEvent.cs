using System.Collections.Generic;
using UnityEngine;
using LoopModding.Core;

namespace LoopModding.Helpers{
    public class TriggerGameEvent : MonoBehaviour
    {
        public GameEvents[] gameEvents;
        AddonManager addonManager;
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
            if(addonManager == null) addonManager = AddonManager.Instance;
            if(addonManager == null) {
                Debug.LogWarning("[TriggerGameEvent] AddonManager instance not found.");
                return;
            }
            if(gameEvents == null || gameEvents.Length == 0) return;
            foreach(GameEvents eventToTrigger in gameEvents){
                TriggerEventsRecursive(eventToTrigger, new HashSet<GameEvents>());
            }
        }

        void TriggerEventsRecursive(GameEvents eventToTrigger, HashSet<GameEvents> visited){
            if(eventToTrigger == null) return;
            if(!visited.Add(eventToTrigger)){
                Debug.LogWarning($"[TriggerGameEvent] Detected cyclic event chain at '{eventToTrigger.eventName}'. Skipping to prevent infinite loop.");
                return;
            }

            if(!string.IsNullOrEmpty(eventToTrigger.eventName)){
                addonManager.TriggerEvent(eventToTrigger.eventName);
            }

            if(eventToTrigger.chainedEvents != null){
                foreach(GameEvents chainedEvent in eventToTrigger.chainedEvents){
                    TriggerEventsRecursive(chainedEvent, visited);
                }
            }

            visited.Remove(eventToTrigger);
        }
    }
}