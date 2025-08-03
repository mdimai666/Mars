namespace EditorJsBlazored.Blocks;

public class BlockHeader : IEditorJsBlock
{
    public string Text { get; set; } = "";
    public int Level { get; set; } = 1;

    public string GetHtml() => $"<h{Level}>{Text}</h{Level}>";
}
