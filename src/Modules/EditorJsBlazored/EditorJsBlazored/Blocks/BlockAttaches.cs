using static EditorJsBlazored.Blocks.BlockImage;

namespace EditorJsBlazored.Blocks;

public class BlockAttaches : IEditorJsBlock
{
    public string Title { get; set; } = "";
    public ImageFileData? File { get; set; }

    public string GetHtml()
    {
        var url = File?.Url;

        var cssClasses = "editorjs block-attaches";

        var attachesIconUrl = "./_content/EditorJsBlazored/img/attaches-icon.svg";
        var downloadIconUrl = "./_content/EditorJsBlazored/img/download-icon.svg";
        var title = string.IsNullOrEmpty(Title) ? File?.FileName : Title;

        var (size, sizeName) = ToHumanizedSize(File?.Size ?? 0);

        //return @$"<img src=""{url}"" class=""{cssClasses}"" alt=""{caption}"" />";
        return $"""
            <div class="cdx-attaches cdx-attaches--with-file {cssClasses}">
                <div class="cdx-attaches__file-icon">
                    <div class="cdx-attaches__file-icon-background">
                        <img src="{attachesIconUrl}" />
                    </div>
                </div>
                <div class="cdx-attaches__file-info">
                    <div class="cdx-attaches__title" data-placeholder="File title" data-empty="false">{title}</div>
                    <div class="cdx-attaches__size" data-size="{sizeName}">{size}</div>
                </div>
                <a class="cdx-attaches__download-button" href="{File?.Url}" target="_blank" rel="nofollow noindex noreferrer">
                    <img src="{downloadIconUrl}" />
                </a></div>
            """;
    }

    internal static (string size, string sizeName) ToHumanizedSize(long size)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
        // show a single decimal place, and no space.
        //return string.Format("{0:0.##} {1}", len, sizes[order]);
        return (string.Format("{0:0.#}", len), sizes[order]);
    }
}
