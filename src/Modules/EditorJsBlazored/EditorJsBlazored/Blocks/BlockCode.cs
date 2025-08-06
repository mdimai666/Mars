namespace EditorJsBlazored.Blocks;

public class BlockCode : IEditorJsBlock
{
    public string Code { get; set; } = "";

    public string GetHtml() => $"<pre class=\"editorjs block-code\"><code>${Code}</code></pre>";
}
