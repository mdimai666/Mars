namespace Mars.Core.Utils;

public static class UrlTool
{
    public static string Combine(params string[] parts)
    {
        if (parts == null || parts.Length == 0)
            return string.Empty;

        var first = parts[0];
        var last = parts[^1];

        bool startsWithSlash = !string.IsNullOrEmpty(first) && first[0] == '/';
        bool endsWithSlash = !string.IsNullOrEmpty(last) && last[^1] == '/';

        var combined = string.Join("/", parts
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p.Trim('/')));

        if (startsWithSlash) combined = "/" + combined;
        if (endsWithSlash && !combined.EndsWith("/")) combined += "/";

        return combined;
    }
}
