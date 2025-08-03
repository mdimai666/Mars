namespace EditorJsBlazored.Blocks;

public class BlockQuote : IEditorJsBlock
{
    public string Text { get; set; } = "";
    public string Caption { get; set; } = "";

    /// <summary>
    /// left | center
    /// </summary>
    public string Alignment { get; set; } = "left";

    static readonly string[] AlignmentValues = ["left", "center"];

    public string GetHtml()
    {
        if (!AlignmentValues.Contains(Alignment))
        {
            Console.Error.WriteLine($"Alignment not valid '{Alignment}'. Must one of [{string.Join(", ", AlignmentValues)}]");
        }
        var cssClasses = "editorjs block-quote";

        if (Alignment == "center")
            return @$"<blockquote class=""{cssClasses}"" style=""margin: auto 0;"">${Text}</blockquote> - ${Caption}";
        else
            return $"<blockquote class=\"{cssClasses}\">${Text}</blockquote> - ${Caption}";

    }
}
