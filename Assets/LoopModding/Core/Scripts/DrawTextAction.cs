using System;
using LoopModding.Core.Runtime;
using SimpleJSON;
using TMPro;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Displays a configurable text element on the mod UI canvas.
    /// </summary>
    public class DrawTextAction : ModApiAction
    {
        public override string ActionName => "DrawText";

        public override void Execute(JSONNode args)
        {
            if (args == null)
            {
                Debug.LogWarning("[MOD] DrawText called without arguments.");
                return;
            }

            string text = args.HasKey("text") ? args["text"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[MOD] DrawText requires a non-empty 'text' argument.");
                return;
            }

            string id = args.HasKey("id") ? args["id"].Value : null;
            float x = args.HasKey("x") ? args["x"].AsFloat : 0.5f;
            float y = args.HasKey("y") ? args["y"].AsFloat : 0.5f;
            bool normalized = args.HasKey("normalized") && args["normalized"].AsBool;
            float duration = args.HasKey("duration") ? Mathf.Max(0f, args["duration"].AsFloat) : 0f;
            bool raycastTarget = args.HasKey("raycastTarget") && args["raycastTarget"].AsBool;

            int fontSize = args.HasKey("fontSize") ? Mathf.Max(1, args["fontSize"].AsInt) : 32;
            string colorArg = args.HasKey("color") ? args["color"].Value : "#FFFFFF";
            string alignmentArg = args.HasKey("alignment") ? args["alignment"].Value : "Center";
            string styleArg = args.HasKey("fontStyle") ? args["fontStyle"].Value : string.Empty;

            float width = args.HasKey("width") ? Mathf.Max(0f, args["width"].AsFloat) : 0f;
            float height = args.HasKey("height") ? Mathf.Max(0f, args["height"].AsFloat) : 0f;
            float pivotX = args.HasKey("pivotX") ? Mathf.Clamp01(args["pivotX"].AsFloat) : 0.5f;
            float pivotY = args.HasKey("pivotY") ? Mathf.Clamp01(args["pivotY"].AsFloat) : 0.5f;

            if (!ModUiRuntime.TryParseColor(colorArg, out Color color))
            {
                Debug.LogWarning($"[MOD] DrawText received an invalid color '{colorArg}'. Falling back to white.");
                color = Color.white;
            }

            TextAlignmentOptions alignment = ParseAlignment(alignmentArg);
            FontStyles fontStyle = ParseFontStyle(styleArg);

            Vector2 position = normalized ? new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y)) : new Vector2(x, y);
            Vector2? size = (width > 0f && height > 0f) ? new Vector2(width, height) : (Vector2?)null;
            Vector2 pivot = new Vector2(pivotX, pivotY);

            ModUiRuntime runtime = ModUiRuntime.EnsureInstance();
            string elementId = runtime.DrawText(
                id,
                text,
                position,
                normalized ? ModUiRuntime.PositionMode.Normalized : ModUiRuntime.PositionMode.Pixel,
                color,
                fontSize,
                alignment,
                fontStyle,
                raycastTarget,
                size,
                pivot,
                duration);

            Debug.Log($"[MOD] DrawText displayed text with id '{elementId}'.");
        }

        private static TextAlignmentOptions ParseAlignment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return TextAlignmentOptions.Center;
            }

            if (Enum.TryParse(value, true, out TextAlignmentOptions alignment))
            {
                return alignment;
            }

            return TextAlignmentOptions.Center;
        }

        private static FontStyles ParseFontStyle(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return FontStyles.Normal;
            }

            FontStyles style = FontStyles.Normal;
            string[] parts = value.Split('|');
            foreach (string part in parts)
            {
                if (Enum.TryParse(part.Trim(), true, out FontStyles parsed))
                {
                    style |= parsed;
                }
            }

            return style;
        }
    }
}
