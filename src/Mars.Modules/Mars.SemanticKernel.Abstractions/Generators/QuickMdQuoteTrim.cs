namespace Mars.SemanticKernel.Host.Shared.Generators;

public static class QuickMdQuoteTrim
{
    /// <summary>
    /// Максимально простой триммер для LLM ответов
    /// </summary>
    public static string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var cleaned = text.Trim();

        // Удаляем ``` в начале (с любым словом после)
        if (cleaned.StartsWith("```"))
        {
            var firstNewLine = cleaned.IndexOf('\n');
            if (firstNewLine > 0)
                cleaned = cleaned.Substring(firstNewLine + 1);
            else
                cleaned = cleaned.Substring(3);
        }

        // Удаляем ``` в конце
        if (cleaned.EndsWith("```"))
            cleaned = cleaned.Substring(0, cleaned.LastIndexOf("```"));

        return cleaned.Trim();
    }
}
