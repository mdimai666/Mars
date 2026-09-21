using System.Collections;
using System.Reflection;
using System.Text.Json;
using Mars.Core.Extensions;

namespace Mars.Nodes.Core;

/// <summary>
/// Builds the debug snapshot JSON: strings are cut by length, structures — by depth and by the first items.
/// A finite depth also guarantees termination on cyclic values.
/// </summary>
public static class NodeDebugSnapshotBuilder
{
    public const int DefaultMaxStringLength = 150;
    public const int DefaultMaxDepth = 4;
    public const int DefaultMaxItems = 50;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static NodeDebugSnapshot Build(NodeMsg msg, string nodeId, int port)
    {
        var truncated = Truncate(msg.AsFullDict(), DefaultMaxDepth, DefaultMaxStringLength, DefaultMaxItems);

        return new NodeDebugSnapshot(nodeId, port, DateTime.Now, JsonSerializer.Serialize(truncated, JsonOptions));
    }

    static object? Truncate(object? value, int depth, int maxStringLength, int maxItems)
    {
        switch (value)
        {
            case null:
                return null;
            case string text:
                return text.TextEllipsis(maxStringLength);
            case Exception ex:
                return $"({ex.GetType().Name}) {ex.Message}".TextEllipsis(maxStringLength);
            case Enum enumValue:
                return enumValue.ToString();
            case bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double
                or decimal or Guid or DateTime or DateTimeOffset or TimeSpan:
                return value;
        }

        if (depth <= 0) return AsJson(value, maxStringLength);

        if (value is IDictionary dictionary)
        {
            var result = new Dictionary<string, object?>();

            foreach (DictionaryEntry entry in dictionary)
            {
                if (result.Count >= maxItems) break;

                result[entry.Key?.ToString() ?? ""] = Truncate(entry.Value, depth - 1, maxStringLength, maxItems);
            }

            return result;
        }

        if (value is IEnumerable enumerable)
        {
            var result = new List<object?>();

            foreach (var item in enumerable)
            {
                if (result.Count >= maxItems) break;

                result.Add(Truncate(item, depth - 1, maxStringLength, maxItems));
            }

            return result;
        }

        return TruncateProperties(value, depth, maxStringLength, maxItems);
    }

    static Dictionary<string, object?> TruncateProperties(object value, int depth, int maxStringLength, int maxItems)
    {
        var result = new Dictionary<string, object?>();

        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (result.Count >= maxItems) break;
            if (!property.CanRead || property.GetIndexParameters().Length != 0) continue;

            object? propertyValue;

            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            result[property.Name] = Truncate(propertyValue, depth - 1, maxStringLength, maxItems);
        }

        return result;
    }

    static string AsJson(object value, int maxStringLength)
    {
        try
        {
            return JsonSerializer.Serialize(value, JsonOptions).TextEllipsis(maxStringLength);
        }
        catch
        {
            return value.ToString()?.TextEllipsis(maxStringLength) ?? "";
        }
    }
}
