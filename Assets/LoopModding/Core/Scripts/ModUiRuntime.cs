using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace LoopModding.Core.Runtime
{
    /// <summary>
    /// Runtime helper used by ModAPI actions to spawn UI elements (texts, images, buttons).
    /// Creates a lightweight canvas automatically when first accessed.
    /// </summary>
    public class ModUiRuntime : MonoBehaviour
    {
        public enum PositionMode
        {
            Pixel,
            Normalized
        }

        private const string DefaultCanvasName = "ModUIRoot";

        public static ModUiRuntime Instance { get; private set; }

        [Header("Canvas Setup")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform imageRoot;
        [SerializeField] private RectTransform textRoot;
        [SerializeField] private RectTransform buttonRoot;

        private readonly Dictionary<string, RawImage> imageRegistry = new();
        private readonly Dictionary<string, bool> imageAspectPreference = new();
        private readonly Dictionary<string, Vector2> imageBaseSize = new();
        private readonly Dictionary<string, TMP_Text> textRegistry = new();
        private readonly Dictionary<string, Button> buttonRegistry = new();

        private readonly Dictionary<string, Coroutine> imageTimers = new();
        private readonly Dictionary<string, Coroutine> textTimers = new();
        private readonly Dictionary<string, Coroutine> buttonTimers = new();

        public static ModUiRuntime EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            ModUiRuntime existing = FindObjectOfType<ModUiRuntime>();
            if (existing != null)
            {
                Instance = existing;
                Instance.Initialize();
                return Instance;
            }

            GameObject runtimeGo = new(DefaultCanvasName);
            DontDestroyOnLoad(runtimeGo);
            Instance = runtimeGo.AddComponent<ModUiRuntime>();
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
            EnsureEventSystem();
            EnsureCanvasHierarchy();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObj = new("ModUI_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObj);
        }

        private void EnsureCanvasHierarchy()
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInChildren<Canvas>();
            }

            if (rootCanvas == null)
            {
                GameObject canvasObj = new("ModUICanvas");
                canvasObj.layer = LayerMask.NameToLayer("UI");
                canvasObj.transform.SetParent(transform, false);

                rootCanvas = canvasObj.AddComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            imageRoot ??= CreateLayerRoot("Images");
            textRoot ??= CreateLayerRoot("Texts");
            buttonRoot ??= CreateLayerRoot("Buttons");
        }

        private RectTransform CreateLayerRoot(string suffix)
        {
            GameObject go = new($"ModUI_{suffix}", typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(rootCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        public string DrawText(
            string desiredId,
            string text,
            Vector2 position,
            PositionMode positionMode,
            Color color,
            int fontSize,
            TextAlignmentOptions alignment,
            FontStyles fontStyle,
            bool raycastTarget,
            Vector2? size,
            Vector2 pivot,
            float durationSeconds)
        {
            EnsureCanvasHierarchy();

            string id = string.IsNullOrWhiteSpace(desiredId) ? Guid.NewGuid().ToString("N") : desiredId;
            TMP_Text element = GetOrCreateText(id);
            element.text = text ?? string.Empty;
            element.color = color;
            element.fontSize = Mathf.Max(1, fontSize);
            element.alignment = alignment;
            element.fontStyle = fontStyle;
            element.raycastTarget = raycastTarget;
            element.enableWordWrapping = true;

            RectTransform rect = element.rectTransform;
            ApplyPosition(rect, position, positionMode, pivot);
            ApplySize(rect, size);

            RestartTimer(textTimers, id, durationSeconds, () => RemoveText(id));

            return id;
        }

        public string ShowImage(
            string desiredId,
            string url,
            Vector2 position,
            PositionMode positionMode,
            Vector2 size,
            Vector2 pivot,
            float rotation,
            Color color,
            bool preserveAspect,
            float durationSeconds)
        {
            EnsureCanvasHierarchy();

            string id = string.IsNullOrWhiteSpace(desiredId) ? Guid.NewGuid().ToString("N") : desiredId;
            RawImage image = GetOrCreateImage(id);
            image.color = color;
            image.raycastTarget = true;
            image.transform.localEulerAngles = new Vector3(0f, 0f, rotation);
            image.texture = null;
            image.uvRect = new Rect(0f, 0f, 1f, 1f);

            RectTransform rect = image.rectTransform;
            ApplyPosition(rect, position, positionMode, pivot);
            ApplySize(rect, size);

            imageAspectPreference[id] = preserveAspect;
            imageBaseSize[id] = size;
            StartCoroutine(LoadImageCoroutine(url, image, id));

            RestartTimer(imageTimers, id, durationSeconds, () => RemoveImage(id));

            return id;
        }

        public string CreateOrUpdateButton(
            string desiredId,
            string label,
            Vector2 position,
            PositionMode positionMode,
            Vector2 size,
            Vector2 pivot,
            Action onClick,
            bool interactable,
            float durationSeconds,
            out Button button)
        {
            EnsureCanvasHierarchy();

            string id = string.IsNullOrWhiteSpace(desiredId) ? Guid.NewGuid().ToString("N") : desiredId;
            button = GetOrCreateButton(id);
            button.interactable = interactable;

            TMP_Text textComponent = button.GetComponentInChildren<TMP_Text>();
            if (textComponent == null)
            {
                textComponent = CreateButtonLabel(button.transform);
            }
            textComponent.text = label ?? "Button";

            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick.Invoke());
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            ApplyPosition(rect, position, positionMode, pivot);
            ApplySize(rect, size);

            RestartTimer(buttonTimers, id, durationSeconds, () => RemoveButton(id));

            return id;
        }

        public void RemoveImage(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (imageTimers.TryGetValue(id, out Coroutine timer))
            {
                StopCoroutine(timer);
                imageTimers.Remove(id);
            }

            if (imageRegistry.TryGetValue(id, out RawImage image))
            {
                Destroy(image.gameObject);
                imageRegistry.Remove(id);
            }

            imageAspectPreference.Remove(id);
            imageBaseSize.Remove(id);
        }

        public void RemoveText(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (textTimers.TryGetValue(id, out Coroutine timer))
            {
                StopCoroutine(timer);
                textTimers.Remove(id);
            }

            if (textRegistry.TryGetValue(id, out TMP_Text text))
            {
                Destroy(text.gameObject);
                textRegistry.Remove(id);
            }
        }

        public void RemoveButton(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (buttonTimers.TryGetValue(id, out Coroutine timer))
            {
                StopCoroutine(timer);
                buttonTimers.Remove(id);
            }

            if (buttonRegistry.TryGetValue(id, out Button button))
            {
                Destroy(button.gameObject);
                buttonRegistry.Remove(id);
            }
        }

        private TMP_Text GetOrCreateText(string id)
        {
            if (textRegistry.TryGetValue(id, out TMP_Text element) && element != null)
            {
                return element;
            }

            GameObject go = new($"ModUIText_{id}");
            go.transform.SetParent(textRoot, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 80f);

            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            textRegistry[id] = text;
            return text;
        }

        private RawImage GetOrCreateImage(string id)
        {
            if (imageRegistry.TryGetValue(id, out RawImage image) && image != null)
            {
                return image;
            }

            GameObject go = new($"ModUIImage_{id}");
            go.transform.SetParent(imageRoot, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(256f, 256f);

            RawImage rawImage = go.AddComponent<RawImage>();
            imageRegistry[id] = rawImage;
            return rawImage;
        }

        private Button GetOrCreateButton(string id)
        {
            if (buttonRegistry.TryGetValue(id, out Button button) && button != null)
            {
                return button;
            }

            GameObject go = new($"ModUIButton_{id}");
            go.transform.SetParent(buttonRoot, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 40f);

            Image background = go.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            Button newButton = go.AddComponent<Button>();
            buttonRegistry[id] = newButton;
            return newButton;
        }

        private TMP_Text CreateButtonLabel(Transform parent)
        {
            GameObject labelGo = new("Label");
            labelGo.transform.SetParent(parent, false);

            RectTransform rect = labelGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = labelGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = false;
            return text;
        }

        private void ApplyPosition(RectTransform rect, Vector2 position, PositionMode mode, Vector2 pivot)
        {
            rect.pivot = pivot;

            switch (mode)
            {
                case PositionMode.Normalized:
                    rect.anchorMin = position;
                    rect.anchorMax = position;
                    rect.anchoredPosition = Vector2.zero;
                    break;
                default:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = position;
                    break;
            }
        }

        private void ApplySize(RectTransform rect, Vector2? size)
        {
            if (size.HasValue && size.Value != Vector2.zero)
            {
                Vector2 value = size.Value;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, value.x));
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, value.y));
            }
        }

        private void RestartTimer(Dictionary<string, Coroutine> map, string id, float durationSeconds, Action onComplete)
        {
            if (map.TryGetValue(id, out Coroutine previous) && previous != null)
            {
                StopCoroutine(previous);
                map.Remove(id);
            }

            if (durationSeconds > 0f)
            {
                Coroutine timer = StartCoroutine(RemoveAfterDelay(durationSeconds, onComplete));
                map[id] = timer;
            }
        }

        private IEnumerator RemoveAfterDelay(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }

        private void ApplyImageAspect(RectTransform rect, Vector2 requestedSize, Texture texture)
        {
            Vector2 size = CalculateAspectSize(requestedSize, texture);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, size.x));
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, size.y));
        }

        private Vector2 CalculateAspectSize(Vector2 requestedSize, Texture texture)
        {
            if (texture == null)
            {
                return requestedSize;
            }

            float texWidth = texture.width;
            float texHeight = texture.height;
            if (texWidth <= 0f || texHeight <= 0f)
            {
                return requestedSize;
            }

            if (requestedSize == Vector2.zero)
            {
                return new Vector2(texWidth, texHeight);
            }

            float aspect = texWidth / texHeight;

            float targetWidth = requestedSize.x;
            float targetHeight = requestedSize.y;

            if (targetWidth <= 0f && targetHeight <= 0f)
            {
                return new Vector2(texWidth, texHeight);
            }

            if (targetWidth <= 0f)
            {
                targetWidth = targetHeight * aspect;
            }
            else if (targetHeight <= 0f)
            {
                targetHeight = targetWidth / aspect;
            }
            else
            {
                float boxAspect = targetWidth / targetHeight;
                if (aspect > boxAspect)
                {
                    targetHeight = targetWidth / aspect;
                }
                else
                {
                    targetWidth = targetHeight * aspect;
                }
            }

            return new Vector2(targetWidth, targetHeight);
        }

        private IEnumerator LoadImageCoroutine(string url, RawImage target, string id)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning($"[ModUI] Invalid image url for id '{id}'.");
                yield break;
            }

            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ModUI] Failed to download image '{url}' for id '{id}': {request.error}");
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            target.texture = texture;
            RectTransform rect = target.rectTransform;
            if (imageAspectPreference.TryGetValue(id, out bool preserve) && preserve)
            {
                Vector2 baseSize = imageBaseSize.TryGetValue(id, out Vector2 requested) ? requested : Vector2.zero;
                ApplyImageAspect(rect, baseSize, texture);
            }
            else if (imageBaseSize.TryGetValue(id, out Vector2 manualSize) && manualSize != Vector2.zero)
            {
                ApplySize(rect, manualSize);
            }
            else if (texture != null)
            {
                ApplySize(rect, new Vector2(texture.width, texture.height));
            }
        }

        public static bool TryParseColor(string colorString, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(colorString))
            {
                return false;
            }

            if (ColorUtility.TryParseHtmlString(colorString, out color))
            {
                return true;
            }

            string[] parts = colorString.Split(',');
            if (parts.Length < 3)
            {
                return false;
            }

            if (float.TryParse(parts[0], out float r) &&
                float.TryParse(parts[1], out float g) &&
                float.TryParse(parts[2], out float b))
            {
                float a = 1f;
                if (parts.Length > 3 && float.TryParse(parts[3], out float parsedAlpha))
                {
                    a = parsedAlpha;
                }

                color = new Color(r, g, b, a);
                return true;
            }

            return false;
        }
    }
}
