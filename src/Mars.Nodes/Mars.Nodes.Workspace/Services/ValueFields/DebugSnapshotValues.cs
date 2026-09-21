using System.Text.Json;

namespace Mars.Nodes.Workspace.Services.ValueFields;

/// <summary>
/// Flat "path → value" view of a debug snapshot: array items get concrete indexes so the path
/// can be inserted into a field as is.
/// </summary>
internal static class DebugSnapshotValues
{
    public const int MaxValueLength = 80;
    public const int MaxArrayItems = 10;
    public const int MaxDepth = 5;

    public static Dictionary<string, string> Flatten(string json)
    {
        var result = new Dictionary<string, string>();

        try
        {
            using var document = JsonDocument.Parse(json);
            Walk(result, "", document.RootElement, 0);
        }
        catch (JsonException)
        {
        }

        return result;
    }

    static void Walk(Dictionary<string, string> result, string path, JsonElement element, int depth)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object when depth < MaxDepth:
                foreach (var property in element.EnumerateObject())
                    Walk(result, Join(path, property.Name), property.Value, depth + 1);
                return;

            case JsonValueKind.Array when depth < MaxDepth:
                var count = 0;

                foreach (var item in element.EnumerateArray())
                {
                    Walk(result, $"{path}[{count}]", item, depth + 1);

                    if (++count >= MaxArrayItems) break;
                }

                result[path] = $"[{count} items]";
                return;

            default:
                result[path] = Shorten(element);
                return;
        }
    }

    static string Join(string path, string name) => path.Length == 0 ? name : $"{path}.{name}";

    static string Shorten(JsonElement element)
    {
        var text = element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : element.ToString();

        return text.Length <= MaxValueLength ? text : text[..MaxValueLength] + "...";
    }
}
