namespace EditorJsBlazored.Blocks;

public class BlockImage : IEditorJsBlock
{
    public string Url { get; set; } = "";
    public string Caption { get; set; } = "";
    public bool Stretched { get; set; }
    public bool WithBorder { get; set; }
    public bool WithBackground { get; set; }
    public ImageFileData? File { get; set; }

    public string GetHtml()
    {
        var caption = string.IsNullOrEmpty(Caption) ? "Image" : Caption;
        var url = File?.Url ?? Url;

        var cssClasses = "editorjs block-image"
                            + (Stretched ? " Stretched" : "")
                            + (WithBorder ? " WithBorder" : "")
                            + (WithBackground ? " WithBackground" : "");

        return @$"<img src=""{url}"" class=""{cssClasses}"" alt=""{caption}"" />";
    }

    public class ImageFileData
    {
        public string Url { get; set; } = "";
        public long Size { get; set; }
        public string? FileId { get; set; }
        public string? FileName { get; set; } = "";
    }
}
