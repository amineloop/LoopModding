using SimpleJSON;

[System.Serializable]
public class ModDefinition
{
    public string modName;
    public string eventName;
    public string action;
    public JSONNode args;

    public bool HasArgs(params string[] keys)
    {
        foreach (var key in keys)
            if (!args.HasKey(key)) return false;
        return true;
    }

    public T GetArg<T>(string key, T defaultValue = default)
    {
        if (!args.HasKey(key)) return defaultValue;
        var node = args[key];

        try
        {
            if (typeof(T) == typeof(float)) return (T)(object)node.AsFloat;
            if (typeof(T) == typeof(int)) return (T)(object)node.AsInt;
            if (typeof(T) == typeof(bool)) return (T)(object)node.AsBool;
            if (typeof(T) == typeof(string)) return (T)(object)node.Value;
        }
        catch { }

        return defaultValue;
    }
}