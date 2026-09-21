using System.Collections;
using System.Globalization;
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

    public const int FullMaxTotalLength = 2_000_000;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static readonly JsonSerializerOptions FullJsonOptions = new(JsonOptions)
    {
        WriteIndented = true,
    };

    public static NodeDebugSnapshot Build(NodeMsg msg, string nodeId, int port)
    {
        var truncated = Truncate(msg.AsFullDict(), DefaultMaxDepth, DefaultMaxStringLength, DefaultMaxItems);

        return new NodeDebugSnapshot(nodeId, port, DateTime.UtcNow,
            JsonSerializer.Serialize(truncated, JsonOptions), Flatten(truncated));
    }

    /// <summary>
    /// Full-object variant for DebugNode: serialize as is, then hard-cut at the total cap —
    /// over it the JSON is cut mid-structure and marked truncated (no per-field walk).
    /// </summary>
    public static NodeDebugFullSnapshot BuildFull(object? value, string nodeId)
    {
        string json;

        try
        {
            json = JsonSerializer.Serialize(value, FullJsonOptions);
        }
        catch (Exception)
        {
            json = JsonSerializer.Serialize(value?.ToString() ?? "", FullJsonOptions);
        }

        var overLimit = json.Length > FullMaxTotalLength;
        if (overLimit)
            json = json[..FullMaxTotalLength] + "\n...[truncated: exceeded 2 MB limit]";

        return new NodeDebugFullSnapshot(nodeId, DateTime.UtcNow, json, overLimit);
    }

    /// <summary>
    /// Flat "path → value" view of the already truncated tree: array items get concrete indexes so the
    /// path can be inserted into a field as is; a collection itself gets a "[N items]" entry.
    /// </summary>
    public static Dictionary<string, string> Flatten(object? tree)
    {
        var result = new Dictionary<string, string>();
        Walk(result, "", tree);
        return result;
    }

    static void Walk(Dictionary<string, string> result, string path, object? value)
    {
        switch (value)
        {
            case null:
                result[path] = "";
                return;

            case Dictionary<string, object?> dictionary:
                foreach (var (key, item) in dictionary)
                    Walk(result, Join(path, key), item);
                return;

            case List<object?> list:
                var index = 0;
                foreach (var item in list)
                    Walk(result, $"{path}[{index++}]", item);
                result[path] = $"[{index} items]";
                return;

            default:
                result[path] = AsText(value);
                return;
        }
    }

    static string Join(string path, string name) => path.Length == 0 ? name : $"{path}.{name}";

    static string AsText(object value) => value switch
    {
        string text => text,
        bool flag => flag ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

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
