using System.Text.RegularExpressions;

namespace MarsDocs.WebApp.Models;

public static class MarkdownMetadataReader
{
    public static Dictionary<string, string> ReadMetadata(string fileContent)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var regex = new Regex(@"^<!--\s*(\w+)\s*:\s*(.*?)\s*-->$");

        using var reader = new StringReader(fileContent);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (line.TrimStart().StartsWith("#")) // Стоп при первом заголовке
                break;

            var match = regex.Match(line);
            if (match.Success)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                dict[key] = value;
            }
        }

        return dict;
    }
}
