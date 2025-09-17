using SimpleJSON;

/// <summary>
/// Describes how an add-on reacts to a specific event.
/// </summary>
[System.Serializable]
public class AddonDefinition
{
    public string addonName;
    public string eventName;
    public string action;
    public JSONNode args;

    public bool HasArgs(params string[] keys)
    {
        if (args == null)
        {
            return false;
        }

        foreach (var key in keys)
        {
            if (!args.HasKey(key))
            {
                return false;
            }
        }

        return true;
    }

    public T GetArg<T>(string key, T defaultValue = default)
    {
        if (args == null || !args.HasKey(key))
        {
            return defaultValue;
        }

        var node = args[key];

        try
        {
            if (typeof(T) == typeof(float)) return (T)(object)node.AsFloat;
            if (typeof(T) == typeof(int)) return (T)(object)node.AsInt;
            if (typeof(T) == typeof(bool)) return (T)(object)node.AsBool;
            if (typeof(T) == typeof(string)) return (T)(object)node.Value;
        }
        catch
        {
        }

        return defaultValue;
    }
}