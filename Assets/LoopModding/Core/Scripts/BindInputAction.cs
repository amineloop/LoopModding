using System;
using LoopModding.Core.Runtime;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Binds a keyboard key and/or UI button to trigger a declarative action.
    /// </summary>
    public class BindInputAction : AddonApiAction
    {
        public override string ActionName => "BindInput";

        public override void Execute(JSONNode args)
        {
            if (args == null)
            {
                Debug.LogWarning("[AddonAPI] BindInput called without arguments.");
                return;
            }

            string actionId = args.HasKey("actionId") ? args["actionId"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(actionId))
            {
                string legacyEvent = args.HasKey("eventName") ? args["eventName"].Value : string.Empty;
                if (!string.IsNullOrWhiteSpace(legacyEvent))
                {
                    Debug.LogWarning("[AddonAPI] BindInput received deprecated 'eventName'. Use 'actionId' instead.");
                    actionId = legacyEvent;
                }
            }

            if (string.IsNullOrWhiteSpace(actionId))
            {
                Debug.LogWarning("[AddonAPI] BindInput requires an 'actionId'.");
                return;
            }

            string id = args.HasKey("id") ? args["id"].Value : actionId;
            string keyArg = args.HasKey("key") ? args["key"].Value : string.Empty;
            KeyCode? key = TryParseKey(keyArg);

            string triggerArg = args.HasKey("trigger") ? args["trigger"].Value : "Down";
            ActionInputBridge.KeyTrigger trigger = ParseTrigger(triggerArg);

            float holdDelay = args.HasKey("holdDelay") ? Mathf.Max(0f, args["holdDelay"].AsFloat) : 0f;
            float repeatInterval = args.HasKey("repeatInterval") ? Mathf.Max(0f, args["repeatInterval"].AsFloat) : 0f;

            string buttonLabel = args.HasKey("buttonLabel") ? args["buttonLabel"].Value : null;
            bool buttonInteractable = !args.HasKey("buttonInteractable") || args["buttonInteractable"].AsBool;
            float buttonDuration = args.HasKey("buttonDuration") ? Mathf.Max(0f, args["buttonDuration"].AsFloat) : 0f;
            bool buttonNormalized = args.HasKey("buttonNormalized") && args["buttonNormalized"].AsBool;
            float buttonX = args.HasKey("buttonX") ? args["buttonX"].AsFloat : 0.5f;
            float buttonY = args.HasKey("buttonY") ? args["buttonY"].AsFloat : 0.15f;
            float buttonWidth = args.HasKey("buttonWidth") ? Mathf.Max(0f, args["buttonWidth"].AsFloat) : 220f;
            float buttonHeight = args.HasKey("buttonHeight") ? Mathf.Max(0f, args["buttonHeight"].AsFloat) : 60f;
            float buttonPivotX = args.HasKey("buttonPivotX") ? Mathf.Clamp01(args["buttonPivotX"].AsFloat) : 0.5f;
            float buttonPivotY = args.HasKey("buttonPivotY") ? Mathf.Clamp01(args["buttonPivotY"].AsFloat) : 0.5f;

            if (key == null && string.IsNullOrWhiteSpace(buttonLabel))
            {
                Debug.LogWarning("[AddonAPI] BindInput requires at least a 'key' or a 'buttonLabel'.");
                return;
            }

            JSONNode payload = args.HasKey("payload") ? args["payload"] : null;

            ActionInputBridge runtime = ActionInputBridge.EnsureInstance();
            ActionInputBridge.InputBindingOptions options = new()
            {
                Id = id,
                ActionId = actionId,
                Payload = payload,
                Key = key,
                Trigger = trigger,
                HoldDelay = holdDelay,
                RepeatInterval = repeatInterval,
                ButtonLabel = buttonLabel,
                ButtonInteractable = buttonInteractable,
                ButtonDuration = buttonDuration,
                ButtonPosition = buttonNormalized
                    ? new Vector2(Mathf.Clamp01(buttonX), Mathf.Clamp01(buttonY))
                    : new Vector2(buttonX, buttonY),
                ButtonPositionMode = buttonNormalized
                    ? AddonUiRuntime.PositionMode.Normalized
                    : AddonUiRuntime.PositionMode.Pixel,
                ButtonSize = new Vector2(buttonWidth, buttonHeight),
                ButtonPivot = new Vector2(buttonPivotX, buttonPivotY)
            };

            string registeredId = runtime.RegisterBinding(options);
            Debug.Log($"[AddonAPI] BindInput registered '{registeredId}' for action '{actionId}'.");
        }

        private static KeyCode? TryParseKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (Enum.TryParse(value, true, out KeyCode result))
            {
                return result;
            }

            Debug.LogWarning($"[AddonAPI] BindInput could not parse key '{value}'.");
            return null;
        }

        private static ActionInputBridge.KeyTrigger ParseTrigger(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, true, out ActionInputBridge.KeyTrigger trigger))
            {
                return trigger;
            }

            return ActionInputBridge.KeyTrigger.Down;
        }
    }
}
