using Mars.Media.Contracts.Files;

namespace Mars.Admin.SiteSpace.Pages.MediaViews;

public static class MediaTextPreview
{
    public const int MaxSize = 512 * 1024;

    static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "md", "log", "json", "xml", "csv", "yaml", "yml", "ini", "conf", "config",
        "sql", "js", "mjs", "ts", "css", "less", "scss", "html", "htm", "ps1", "sh", "bat",
    };

    public static bool IsPreviewable(FileListItemResponse file)
        => TextExtensions.Contains(file.Ext) && file.Size <= MaxSize;

    public static bool IsOversized(FileListItemResponse file)
        => TextExtensions.Contains(file.Ext) && file.Size > MaxSize;
}

/// <summary>Результат window.blazor_fetch_text: текст + фактическая кодировка (utf-8 | windows-1251).</summary>
public record TextPreviewResult(string Text, string Encoding);
