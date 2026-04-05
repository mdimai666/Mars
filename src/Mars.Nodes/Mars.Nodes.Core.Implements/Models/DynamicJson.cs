using System.Dynamic;
using System.Text.Json.Nodes;

namespace Mars.Nodes.Core.Implements.Models;

public class DynamicJson : DynamicObject
{
    private readonly JsonNode? _node;

    public DynamicJson(JsonNode? node)
    {
        _node = node;
    }

    // Чтение свойства: obj.name
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (_node is JsonObject obj && obj.TryGetPropertyValue(binder.Name, out var value))
        {
            result = Wrap(value);
            return true;
        }

        result = null;
        return true;
    }

    // Запись свойства: obj.name = value
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (_node is not JsonObject obj)
            return false;

        obj[binder.Name] = WrapToNode(value);
        return true;
    }

    // Индексатор: arr[0]
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        if (_node is JsonArray arr && indexes.Length == 1 && indexes[0] is int index)
        {
            if (index >= 0 && index < arr.Count)
            {
                result = Wrap(arr[index]);
                return true;
            }
        }

        result = null;
        return true;
    }

    // Запись в массив: arr[0] = value
    public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
    {
        if (_node is not JsonArray arr || indexes.Length != 1 || indexes[0] is not int index)
            return false;

        while (arr.Count <= index)
            arr.Add(null);

        arr[index] = WrapToNode(value);
        return true;
    }

    // Вспомогательное

    //private static object? Wrap(JsonNode? node)
    //{
    //    return node switch
    //    {
    //        null => null,
    //        JsonValue v => v.GetValue<object>(),
    //        _ => new DynamicJson(node)
    //    };
    //}

    private static object? Wrap(JsonNode? node)
    {
        if (node is JsonValue v)
        {
            if (v.TryGetValue<int>(out var i)) return i;
            if (v.TryGetValue<double>(out var d)) return d;
            if (v.TryGetValue<bool>(out var b)) return b;
            if (v.TryGetValue<string>(out var s)) return s;

            return v.GetValue<object>();
        }

        if (node is not null)
            return new DynamicJson(node);

        return null;
    }

    private static JsonNode? WrapToNode(object? value)
    {
        return value switch
        {
            null => null,
            JsonNode node => node,
            DynamicJson dj => dj._node,
            _ => JsonValue.Create(value)
        };
    }
}
