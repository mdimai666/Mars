using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/DirReadNode/DirReadNode{.lang}.md")]
[Display(GroupName = "storage")]
public class DirReadNode : Node
{
    public string DirPath { get; set; } = "";

    [Display(Description = "* - все; .txt,.json - только два формата")]
    public string Pattern { get; set; } = "*";

    [Display(Description = "1 - только корневые; 0 - все вложенные файлы; 2 - включая один вложенный")]
    public int MaxDepth { get; set; } = 1;

    [Display(Description = "Возвращает только относительные")]
    public bool ReturnRelativePath { get; set; } = true;

    [Display(Description = "Использует найденный .gitignore")]
    public bool UseRootGitIgnore { get; set; }

    public DirReadNode()
    {
        Inputs = [new()];
        Color = "#deb887";
        Outputs = [new NodeOutput()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/file-48.png";
    }

}
