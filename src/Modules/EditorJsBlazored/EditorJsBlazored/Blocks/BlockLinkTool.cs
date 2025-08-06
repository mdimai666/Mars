namespace EditorJsBlazored.Blocks;

public class BlockLinkTool : IEditorJsBlock
{
    public string Link { get; set; } = "";
    public LinkToolMetaData? Meta { get; set; }

    public string GetHtml()
    {
        var title = Meta?.Title ?? Link;
        return $"""<a href="{Link}">{title}</p>""";
    }
}

public class LinkToolMetaData
{
    public string? Image { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}
