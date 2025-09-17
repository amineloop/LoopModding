using System;
using System.Collections.Generic;
using LoopModding.Core.API;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace LoopModding.Core.Runtime
{
    /// <summary>
    /// Binds input sources (keyboard/UI) to declarative actions handled by the ActionManager.
    /// </summary>
    public class ActionInputBridge : MonoBehaviour
    {
        public enum KeyTrigger
        {
            Down,
            Up,
            Held
        }

        private class InputBinding
        {
            public string Id;
            public string ActionId;
            public JSONNode Payload;
            public KeyCode? Key;
            public KeyTrigger Trigger;
            public float HoldDelay;
            public float RepeatInterval;
            public float NextFireTime;
            public Button Button;
        }

        public struct InputBindingOptions
        {
            public string Id;
            public string ActionId;
            public JSONNode Payload;
            public KeyCode? Key;
            public KeyTrigger Trigger;
            public float HoldDelay;
            public float RepeatInterval;
            public string ButtonLabel;
            public Vector2 ButtonPosition;
            public AddonUiRuntime.PositionMode ButtonPositionMode;
            public Vector2 ButtonSize;
            public Vector2 ButtonPivot;
            public bool ButtonInteractable;
            public float ButtonDuration;
        }

        private const string RuntimeName = "ActionInputBridge";

        public static ActionInputBridge Instance { get; private set; }

        private readonly Dictionary<string, InputBinding> bindings = new();

        public static ActionInputBridge EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            ActionInputBridge existing = FindObjectOfType<ActionInputBridge>();
            if (existing != null)
            {
                Instance = existing;
                Instance.Initialize();
                return Instance;
            }

            GameObject go = new(RuntimeName);
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ActionInputBridge>();
            Instance.Initialize();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            ActionManager.EnsureInstance();
        }

        private void Update()
        {
            foreach (InputBinding binding in bindings.Values)
            {
                if (binding == null || string.IsNullOrEmpty(binding.ActionId))
                {
                    continue;
                }

                if (binding.Key.HasValue)
                {
                    EvaluateKey(binding);
                }
            }
        }

        private void EvaluateKey(InputBinding binding)
        {
            KeyCode key = binding.Key.Value;
            switch (binding.Trigger)
            {
                case KeyTrigger.Down:
                    if (Input.GetKeyDown(key))
                    {
                        TriggerBinding(binding);
                    }

                    break;
                case KeyTrigger.Up:
                    if (Input.GetKeyUp(key))
                    {
                        TriggerBinding(binding);
                    }

                    break;
                case KeyTrigger.Held:
                    if (Input.GetKeyDown(key))
                    {
                        binding.NextFireTime = Time.time + Mathf.Max(0f, binding.HoldDelay);
                    }

                    if (Input.GetKey(key) && Time.time >= binding.NextFireTime)
                    {
                        TriggerBinding(binding);
                        if (binding.RepeatInterval > 0f)
                        {
                            binding.NextFireTime = Time.time + binding.RepeatInterval;
                        }
                        else
                        {
                            binding.NextFireTime = float.PositiveInfinity;
                        }
                    }

                    if (Input.GetKeyUp(key))
                    {
                        binding.NextFireTime = 0f;
                    }

                    break;
            }
        }

        private void TriggerBinding(InputBinding binding)
        {
            if (string.IsNullOrEmpty(binding.ActionId))
            {
                return;
            }

            JSONNode payloadClone = binding.Payload != null ? binding.Payload.Clone() : null;
            ActionManager.EnsureInstance().TriggerAction(binding.ActionId, payloadClone);
        }

        public string RegisterBinding(InputBindingOptions options)
        {
            string id = string.IsNullOrWhiteSpace(options.Id) ? Guid.NewGuid().ToString("N") : options.Id;
            InputBinding binding = GetOrCreateBinding(id);

            binding.ActionId = options.ActionId;
            binding.Payload = options.Payload != null ? options.Payload.Clone() : null;
            binding.Key = options.Key;
            binding.Trigger = options.Trigger;
            binding.HoldDelay = Mathf.Max(0f, options.HoldDelay);
            binding.RepeatInterval = Mathf.Max(0f, options.RepeatInterval);
            binding.NextFireTime = 0f;

            if (!string.IsNullOrWhiteSpace(options.ButtonLabel))
            {
                AddonUiRuntime uiRuntime = AddonUiRuntime.EnsureInstance();
                string buttonId = uiRuntime.CreateOrUpdateButton(
                    id,
                    options.ButtonLabel,
                    options.ButtonPosition,
                    options.ButtonPositionMode,
                    options.ButtonSize,
                    options.ButtonPivot,
                    () => TriggerBinding(binding),
                    options.ButtonInteractable,
                    options.ButtonDuration,
                    out Button button);

                binding.Button = button;
                binding.Button.name = $"ActionUIButton_{buttonId}";
            }
            else if (binding.Button != null)
            {
                AddonUiRuntime.EnsureInstance().RemoveButton(id);
                binding.Button = null;
            }

            bindings[id] = binding;
            return id;
        }

        public bool UnregisterBinding(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (!bindings.TryGetValue(id, out InputBinding binding))
            {
                return false;
            }

            if (binding.Button != null)
            {
                AddonUiRuntime.EnsureInstance().RemoveButton(id);
            }

            bindings.Remove(id);
            return true;
        }

        private InputBinding GetOrCreateBinding(string id)
        {
            if (bindings.TryGetValue(id, out InputBinding binding))
            {
                return binding;
            }

            binding = new InputBinding { Id = id };
            bindings[id] = binding;
            return binding;
        }
    }
}
