using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace Mars.Nodes.Core.Implements.Extensions;

public static class HttpContextResponseExtensions
{
    public static async Task<JsonNode?> TryReadJsonAsync(
        this HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var request = context.Request;

        // Нет тела
        if (request.ContentLength is 0 or null)
            return null;

        // Быстрый фильтр по Content-Type (не гарантия!)
        if (request.ContentType is not null &&
            !request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) &&
            !request.ContentType.StartsWith("application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Включаем буферизацию, чтобы не сломать pipeline
        request.EnableBuffering();

        try
        {
            request.Body.Position = 0;

            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms, cancellationToken);

            if (ms.Length == 0)
                return null;

            var buffer = ms.ToArray();

            // Строгая проверка JSON без DOM
            if (!IsValidJson(buffer))
                return null;

            return JsonNode.Parse(buffer);
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static bool IsValidJson(ReadOnlySpan<byte> utf8Json)
    {
        var reader = new Utf8JsonReader(
            utf8Json,
            new JsonReaderOptions
            {
                //AllowTrailingCommas = true,
                //CommentHandling = JsonCommentHandling.Skip,
            });

        try
        {
            while (reader.Read()) { }
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static async Task<string> GetRequestBodyAsStringAsync(this HttpContext context, Encoding? encoding = null)
    {
        var request = context.Request;
        encoding ??= Encoding.UTF8;

        request.EnableBuffering();

        request.Body.Position = 0;

        using var reader = new StreamReader(
            request.Body,
            encoding,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        return body;
    }

}
