using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Runtime.Values;

/// <summary>
/// JSON → PxValue: конвертация начальных переменных запуска
/// (PxRunRequest.InitialVariables) и обмен объектными значениями с хостом.
/// </summary>
public static class PxValueJson
{
    /// <summary>
    /// null → Null; true/false, число, строка — примитивы; объект → PxObjectValue;
    /// массив → PxListValue (рекурсивно).
    /// </summary>
    public static PxValue FromJson(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return PxNullValue.Instance;

            case JsonObject jsonObject:
            {
                var members = new Dictionary<string, PxValue>(StringComparer.Ordinal);
                foreach (var (name, value) in jsonObject)
                    members[name] = FromJson(value);
                return new PxObjectValue(members);
            }

            case JsonArray jsonArray:
                return new PxListValue(jsonArray.Select(FromJson).ToList());

            case JsonValue jsonValue:
                if (jsonValue.TryGetValue(out bool boolean))
                    return new PxBooleanValue(boolean);
                if (jsonValue.TryGetValue(out double number))
                    return new PxNumberValue(number);
                if (jsonValue.TryGetValue(out string? text))
                    return new PxStringValue(text ?? "");
                throw new InvalidOperationException($"Unsupported JSON value: {jsonValue.ToJsonString()}");

            default:
                throw new InvalidOperationException($"Unsupported JSON node: {node.ToJsonString()}");
        }
    }
}
