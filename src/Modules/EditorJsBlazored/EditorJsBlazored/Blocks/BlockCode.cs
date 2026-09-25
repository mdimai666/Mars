using System.Net;

namespace EditorJsBlazored.Blocks;

public class BlockCode : IEditorJsBlock
{
    public string Code { get; set; } = "";

    public string GetHtml() => $"<pre class=\"editorjs block-code\"><code>{WebUtility.HtmlEncode(Code)}</code></pre>";
}
