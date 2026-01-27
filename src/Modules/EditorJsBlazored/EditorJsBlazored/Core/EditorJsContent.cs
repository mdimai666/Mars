using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    public static EditorJsContent FromJsonAutoConvertToBlocks(string input, out bool isReplaced)
    {
        try
        {
            isReplaced = false;

            if (string.IsNullOrWhiteSpace(input))
                return new EditorJsContent();

            return FromJson(input);
        }
        catch (JsonException)
        {
            isReplaced = true;

            if (LooksLikeHtml(input))
                return WrapHtmlInRawBlock(input);

            return CreateParagraps(
                SplitToParagraphs(input)
            );
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

    public static EditorJsContent CreateParagraps(string[] paragraps)
    {
        return new EditorJsContent
        {
            Blocks = paragraps.Select(p => new EditorContentBlock
            {
                Type = "paragraph",
                Data = new BlockParagraph
                {
                    Text = WebUtility.HtmlEncode(p)
                }
            }).ToArray()
        };
    }

    private static bool LooksLikeHtml(string text)
    {
        // быстрый cheap-check
        if (!text.Contains('<') || !text.Contains('>'))
            return false;

        // нормальные html-теги
        return Regex.IsMatch(
            text,
            @"<\s*(p|div|br|span|a|img|ul|ol|li|h[1-6]|blockquote|pre|code|table|tr|td|th)\b",
            RegexOptions.IgnoreCase);
    }

    private static string[] SplitToParagraphs(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
    }
}
