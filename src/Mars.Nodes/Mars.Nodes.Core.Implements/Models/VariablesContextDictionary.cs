namespace Mars.Nodes.Core.Implements.Models;

public class VariablesContextDictionary
{
    protected Dictionary<string, object?> values = new();

    //Remote as Redis

    public void SetValue<T>(string key, T value)
    {
        if (values.ContainsKey(key))
            values[key] = value;
        else
            values.Add(key, value);
    }

    public T? GetValue<T>(string key)
    {
        if (values.TryGetValue(key, out object? _value))
        {
            return (T?)_value;
        }
        return default;
    }

    public object? GetValue(string key)
    {
        if (values.TryGetValue(key, out object? _value))
        {
            return _value;
        }
        return default;
    }

    public Dictionary<string, object?>.KeyCollection Keys => values.Keys;

    public int Count => values.Count;

    public void Clear() => values.Clear();

    public void Remove(string key) => values.Remove(key);

    public bool TryGetValue(string key, out object? value)
    {
        return values.TryGetValue(key, out value);
    }
}
