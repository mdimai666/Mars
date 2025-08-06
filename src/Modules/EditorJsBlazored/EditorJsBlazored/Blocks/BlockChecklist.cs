namespace EditorJsBlazored.Blocks;

public class BlockChecklist : IEditorJsBlock
{
    public ChecklistItemData[] Items { get; set; } = [];

    public string GetHtml()
    {
        return string.Join('\n', Items.Select(x => x.GetHtml()));
    }

    public class ChecklistItemData
    {
        public string Text { get; set; } = "";
        public bool Checked { get; set; }

        public string GetHtml()
        {
            return @$"<label><input type=""checkbox"" {(Checked ? "checked" : "")} /> - {Text}</label>";
        }

    }
}
