using UnityEngine;

[CreateAssetMenu(fileName = "NewModEvent", menuName = "Modding/Mod Event")]
public class GameEvents : ScriptableObject
{
    public string eventName;

    [TextArea(2, 5)]  // 👈 Ça crée une zone de texte redimensionnable (2 lignes min, 5 max)
    public string devNote;

    [Tooltip("Other events to trigger after this one.")]
    public GameEvents[] chainedEvents;
}