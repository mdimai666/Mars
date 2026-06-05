using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/SplitNode/SplitNode{.lang}.md")]
[Display(GroupName = "function")]
public class SplitNode : Node
{
    /// <summary>
    /// Разделитель для строк. По умолчанию перенос строки.
    /// Если оставить пустым ("" или null), строка будет разделена по отдельным символам (буквам).
    /// </summary>
    public string Delimiter { get; set; } = "\\n";

    public SplitNode()
    {
        Color = "#E0D870";
        Icon = "_content/Mars.Nodes.Workspace/nodes/option.svg";
        Inputs = [new()];
        Outputs = [new()];
    }

}
