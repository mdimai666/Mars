using System.Text;

namespace EditorJsBlazored.Blocks;

public class BlockList : IEditorJsBlock // TODO: nested
{
    public string[] Items { get; set; } = [];

    /// <summary>
    ///     style: 'ordered' | 'unordered';
    /// </summary>
    public string Style { get; set; } = "unordered";

    public string GetHtml()
    {
        var listStyle = Style == "unordered" ? "ul" : "ol";
        var sb = new StringBuilder();

        sb.AppendLine($"<{listStyle}>");
        foreach (var item in Items)
        {
            sb.AppendLine(item);
        }
        sb.AppendLine($"</{listStyle}>");
        return sb.ToString();

    }
}
