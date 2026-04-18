using System.Text.Json;

namespace Mars.SemanticKernel.Host.Shared.Generators;

public static class LlmResponseTrimmer
{
    /// <summary>
    /// Безопасно извлекает JSON из ответа LLM, не обрезая остальной текст
    /// </summary>
    public static string TrimResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return response;

        // Сначала пробуем найти валидный JSON в начале или конце
        var json = TryExtractValidJson(response);
        if (json != null)
            return json;

        // Если JSON не нашли, возвращаем оригинал
        return response.Trim();
    }

    /// <summary>
    /// Пытается извлечь валидный JSON, проверяя баланс скобок
    /// </summary>
    private static string? TryExtractValidJson(string text)
    {
        // Ищем позиции где может начинаться JSON
        var startPositions = new List<int>();

        // Ищем { или [ как начало
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{' || text[i] == '[')
            {
                // Проверяем, что перед символом не обратный слэш (не экранирован)
                if (i == 0 || text[i - 1] != '\\')
                    startPositions.Add(i);
            }
        }

        // Пробуем каждый потенциальный старт
        foreach (var start in startPositions)
        {
            var end = FindMatchingBracket(text, start);
            if (end > start)
            {
                var candidate = text.Substring(start, end - start + 1);
                if (IsValidJson(candidate))
                    return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Находит парную закрывающую скобку для JSON
    /// </summary>
    private static int FindMatchingBracket(string text, int startIndex)
    {
        char openBracket = text[startIndex];
        char closeBracket = openBracket == '{' ? '}' : ']';

        int depth = 0;
        bool inString = false;
        bool escapeNext = false;

        for (int i = startIndex; i < text.Length; i++)
        {
            char c = text[i];

            if (escapeNext)
            {
                escapeNext = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escapeNext = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == openBracket)
                    depth++;
                else if (c == closeBracket)
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Проверяет валидность JSON
    /// </summary>
    private static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using (JsonDocument.Parse(json))
                return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Удаляет markdown-блоки кода, но сохраняет остальной текст
    /// </summary>
    public static string RemoveMarkdownCodeBlocks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Удаляем только полные блоки кода, не трогая остальной текст
        var result = text;

        // Паттерн для ```json ... ``` - нежадный, но с проверкой границ
        var patterns = new[]
        {
            @"```json\s+([\s\S]+?)\s+```",  // Без жадности
            @"```\s+([\s\S]+?)\s+```",
            @"````json\s+([\s\S]+?)\s+````",
            @"````\s+([\s\S]+?)\s+````"
        };

        foreach (var pattern in patterns)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(result, pattern,
                System.Text.RegularExpressions.RegexOptions.Multiline);

            // Заменяем только если внутри валидный JSON
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var content = match.Groups[1].Value.Trim();
                if (IsValidJson(content))
                {
                    result = result.Replace(match.Value, content);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Извлекает JSON из большого текста, игнорируя остальной код
    /// </summary>
    public static string ExtractJsonFromText(string text)
    {
        // Сначала удаляем markdown-обертки
        var withoutMarkdown = RemoveMarkdownCodeBlocks(text);

        // Ищем JSON в тексте
        var json = TryExtractValidJson(withoutMarkdown);
        if (json != null)
            return json;

        // Если не нашли, пробуем найти что-то похожее на JSON
        return ExtractJsonWithFallback(withoutMarkdown);
    }

    /// <summary>
    /// Fallback-метод для извлечения JSON с помощью регулярных выражений (безопасная версия)
    /// </summary>
    private static string ExtractJsonWithFallback(string text)
    {
        // Ищем JSON объект с проверкой границ
        var patterns = new[]
        {
            // JSON объект
            @"(?<!\\)\{(?:[^{}]|(?<!\\)\{(?:[^{}]|(?<!\\)\{(?:[^{}]|(?<!\\)\{(?:[^{}]|(?<!\\)\{(?:[^{}]|(?<!\\)\{[^{}]*\})*\})*\})*\})*\}",
            // JSON массив
            @"(?<!\\)\[(?:[^\[\]]|(?<!\\)\[(?:[^\[\]]|(?<!\\)\[(?:[^\[\]]|(?<!\\)\[(?:[^\[\]]|(?<!\\)\[[^\[\]]*\])*\])*\])*\]"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern);
            if (match.Success)
            {
                var candidate = match.Value;
                if (IsValidJson(candidate))
                    return candidate;
            }
        }

        return text;
    }

    /// <summary>
    /// Полный pipeline очистки ответа LLM
    /// </summary>
    public static string CleanLlmResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return response;

        // Шаг 1: Удаляем markdown-блоки
        var step1 = RemoveMarkdownCodeBlocks(response);

        // Шаг 2: Извлекаем JSON
        var step2 = ExtractJsonFromText(step1);

        // Шаг 3: Очищаем от пояснений
        var step3 = RemoveExplanatoryPrefix(step2);

        return step3.Trim();
    }

    private static string RemoveExplanatoryPrefix(string text)
    {
        if (text.StartsWith("{") || text.StartsWith("["))
            return text;

        // Ищем первый JSON символ
        var jsonStart = -1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{' || text[i] == '[')
            {
                jsonStart = i;
                break;
            }
        }

        if (jsonStart > 0)
            return text.Substring(jsonStart);

        return text;
    }

    /// <summary>
    /// Десериализует ответ в указанный тип
    /// </summary>
    public static T? DeserializeFromResponse<T>(string response, JsonSerializerOptions? options = null)
    {
        var cleaned = CleanLlmResponse(response);
        try
        {
            return JsonSerializer.Deserialize<T>(cleaned, options);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON Deserialization failed: {ex.Message}");
            Console.WriteLine($"Cleaned JSON (first 500 chars): {cleaned.Substring(0, Math.Min(500, cleaned.Length))}");
            throw;
        }
    }
}
