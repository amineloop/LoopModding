using System.Collections.Generic;
using SimpleJSON;

namespace LoopModding.Core
{
    /// <summary>
    /// Declarative description of an action available to bridges and add-ons.
    /// </summary>
    [System.Serializable]
    public class ActionDefinition
    {
        public string ActionId { get; set; }
        public List<string> EventNames { get; set; } = new();
        public string Description { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; }
        public float Cooldown { get; set; }
        public bool RequiresUnlock { get; set; }
        public JSONNode DefaultPayload { get; set; } = new JSONObject();
    }
}
