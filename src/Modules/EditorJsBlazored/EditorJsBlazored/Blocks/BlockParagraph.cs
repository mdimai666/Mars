namespace EditorJsBlazored.Blocks;

public class BlockParagraph : IEditorJsBlock
{
    public string Text { get; set; } = "";

    public string GetHtml() => $"<p>{Text}</p>";
}
