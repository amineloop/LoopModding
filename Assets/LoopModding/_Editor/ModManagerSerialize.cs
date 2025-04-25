#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using LoopModding.Core;
using LoopModding.Core.API;

/// <summary>
/// Custom inspector for ModManager showing loaded mods, parameters and ModAPI actions.
/// </summary>
[CustomEditor(typeof(ModManager))]
public class ModManagerEditor : Editor
{
    private string eventName;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ModManager manager = (ModManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🧩 Loaded Mods", EditorStyles.boldLabel);

        if (Application.isPlaying && manager.HasLoadedMods())
        {
            foreach (var entry in manager.GetLoadedMods())
            {
                EditorGUILayout.LabelField($"Event: {entry.Key} ({entry.Value.Count} mod(s))");
                foreach (var mod in entry.Value)
                {
                    EditorGUILayout.LabelField($"  → {mod.modName} | Action: {mod.action}");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No mods loaded or not in play mode.", MessageType.Info);
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
        EditorGUILayout.LabelField("⚙️ Registered ModAPI Actions", EditorStyles.boldLabel);

        if (ModAPI.HasRegisteredActions())
        {
            foreach (var actionName in ModAPI.GetRegisteredActions())
            {
                EditorGUILayout.LabelField($"→ {actionName}");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No actions registered in ModAPI.", MessageType.Warning);
        }
    }
}
#endif