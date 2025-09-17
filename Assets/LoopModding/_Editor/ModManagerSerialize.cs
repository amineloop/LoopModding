#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using LoopModding.Core;
using LoopModding.Core.API;

/// <summary>
/// Custom inspector for AddonManager showing loaded add-ons, parameters and AddonAPI actions.
/// </summary>
[CustomEditor(typeof(AddonManager))]
public class AddonManagerEditor : Editor
{
    private string eventName;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AddonManager manager = (AddonManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🧩 Loaded Add-ons", EditorStyles.boldLabel);

        if (Application.isPlaying && manager.HasLoadedAddons())
        {
            foreach (var entry in manager.GetLoadedAddons())
            {
                EditorGUILayout.LabelField($"Event: {entry.Key} ({entry.Value.Count} add-on(s))");
                foreach (var addon in entry.Value)
                {
                    EditorGUILayout.LabelField($"  → {addon.addonName} | Action: {addon.action}");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No add-ons loaded or not in play mode.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🧪 Trigger Event", EditorStyles.boldLabel);
        eventName = EditorGUILayout.TextField("Event Name", eventName);
        if (GUILayout.Button("Trigger"))
        {
            manager.TriggerEvent(eventName);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("📦 Parameters", EditorStyles.boldLabel);
        if (Application.isPlaying && manager.HasLoadedParameters())
        {
            foreach (var entry in manager.GetLoadedParameters())
            {
                string valueStr = entry.Value.ToString();
                EditorGUILayout.LabelField($"Key: {entry.Key}", $"Value: {valueStr}");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("⚙️ Registered AddonAPI Actions", EditorStyles.boldLabel);

        if (AddonAPI.HasRegisteredActions())
        {
            foreach (var actionName in AddonAPI.GetRegisteredActions())
            {
                EditorGUILayout.LabelField($"→ {actionName}");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No actions registered in AddonAPI.", MessageType.Warning);
        }
    }
}
#endif
