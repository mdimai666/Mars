using System.Text.Encodings.Web;
using System.Text.Json;
using EditorJsBlazored.Blocks;

namespace EditorJsBlazored.Core;

public class EditorJsContent
{

    /// <summary>
    /// Blocks compoing the content.
    /// </summary>
    public EditorContentBlock[] Blocks { get; init; } = [];

    /// <summary>
    /// Time when block was created.
    /// </summary>
    public long Time { get; set; } = 0; // 1550476186479,

    /// <summary>
    /// Content version
    /// </summary>
    public string Version { get; set; } = "0.0.0"; // "2.8.1"

    public DateTimeOffset TimeAsDateTime => DateTimeOffset.FromUnixTimeMilliseconds(Time);

    internal static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static readonly JsonSerializerOptions _jsonSerializerOptionsIdented = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public string ToJson(bool ident = false)
    {
        return JsonSerializer.Serialize(this, ident ? _jsonSerializerOptionsIdented : _jsonSerializerOptions);
    }

    public static EditorJsContent FromJson(string json)
    {
        return JsonSerializer.Deserialize<EditorJsContent>(json, _jsonSerializerOptions)!;
    }

    public static EditorJsContent FromJsonAutoConvertToRawBlockOnException(string jsonOrHtml, out bool isReplaced)
    {
        try
        {
            isReplaced = false;
            if (string.IsNullOrEmpty(jsonOrHtml)) return new();
            return FromJson(jsonOrHtml);
        }
        catch (JsonException)
        {
            isReplaced = true;
            return WrapHtmlInRawBlock(jsonOrHtml);
        }
    }

    public static EditorJsContent WrapHtmlInRawBlock(string html)
    {
        return new EditorJsContent
        {
            Blocks = [new EditorContentBlock
            {
                Type = "raw",
                Data = new BlockRaw
                {
                    Html = html
                }
            }]
        };
    }
}
