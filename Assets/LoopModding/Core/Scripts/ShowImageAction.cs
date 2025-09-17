using LoopModding.Core.Runtime;
using SimpleJSON;
using UnityEngine;

namespace LoopModding.Core.API
{
    /// <summary>
    /// Downloads an image from a remote URL and displays it on the mod UI canvas.
    /// </summary>
    public class ShowImageAction : ModApiAction
    {
        public override string ActionName => "ShowImage";

        public override void Execute(JSONNode args)
        {
            if (args == null)
            {
                Debug.LogWarning("[MOD] ShowImage called without arguments.");
                return;
            }

            string url = args.HasKey("url") ? args["url"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning("[MOD] ShowImage requires a 'url' argument.");
                return;
            }

            string id = args.HasKey("id") ? args["id"].Value : null;
            float x = args.HasKey("x") ? args["x"].AsFloat : 0.5f;
            float y = args.HasKey("y") ? args["y"].AsFloat : 0.5f;
            bool normalized = args.HasKey("normalized") && args["normalized"].AsBool;
            float width = args.HasKey("width") ? Mathf.Max(0f, args["width"].AsFloat) : 256f;
            float height = args.HasKey("height") ? Mathf.Max(0f, args["height"].AsFloat) : 256f;
            float pivotX = args.HasKey("pivotX") ? Mathf.Clamp01(args["pivotX"].AsFloat) : 0.5f;
            float pivotY = args.HasKey("pivotY") ? Mathf.Clamp01(args["pivotY"].AsFloat) : 0.5f;
            float rotation = args.HasKey("rotation") ? args["rotation"].AsFloat : 0f;
            float duration = args.HasKey("duration") ? Mathf.Max(0f, args["duration"].AsFloat) : 0f;
            bool preserveAspect = args.HasKey("preserveAspect") && args["preserveAspect"].AsBool;

            Color color = Color.white;
            if (args.HasKey("color") && !ModUiRuntime.TryParseColor(args["color"].Value, out color))
            {
                Debug.LogWarning($"[MOD] ShowImage received an invalid color '{args["color"].Value}'. Falling back to white.");
                color = Color.white;
            }

            if (args.HasKey("alpha"))
            {
                color.a = Mathf.Clamp01(args["alpha"].AsFloat);
            }

            Vector2 position = normalized ? new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y)) : new Vector2(x, y);
            Vector2 size = new Vector2(width, height);
            Vector2 pivot = new Vector2(pivotX, pivotY);

            ModUiRuntime runtime = ModUiRuntime.EnsureInstance();
            string elementId = runtime.ShowImage(
                id,
                url,
                position,
                normalized ? ModUiRuntime.PositionMode.Normalized : ModUiRuntime.PositionMode.Pixel,
                size,
                pivot,
                rotation,
                color,
                preserveAspect,
                duration);

            Debug.Log($"[MOD] ShowImage displaying '{url}' with id '{elementId}'.");
        }
    }
}
