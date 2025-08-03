namespace EditorJsBlazored.Blocks;

public class BlockRaw : IEditorJsBlock
{
    public string Html { get; set; } = "";

    public string GetHtml() => Html;
}
