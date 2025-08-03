using System.Text;

namespace EditorJsBlazored.Blocks;

public class BlockTable : IEditorJsBlock
{
    public int Rows { get; set; }
    public int Cols { get; set; }
    public string[][] Content { get; set; } = [];
    public bool WithHeading { get; set; }

    public string GetHtml()
    {
        var sb = new StringBuilder();
        var cssClasses = "editorjs block-table";

        sb.AppendLine($"<table class=\"{cssClasses}\">");

        int rowIndex = 0;
        foreach (var row in Content)
        {
            var isHeadingRow = (WithHeading && rowIndex == 0);

            sb.AppendLine(isHeadingRow ? "<th>" : "<tr>");
            foreach (var col in row)
            {
                sb.AppendLine($"<td>{col}</td>");
            }
            sb.AppendLine(isHeadingRow ? "</th>" : "</tr>");
            rowIndex++;
        }

        sb.AppendLine("<table>");

        return sb.ToString();
    }
}
