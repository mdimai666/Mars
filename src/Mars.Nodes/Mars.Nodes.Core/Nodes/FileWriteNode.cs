using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/FileWriteNode/FileWriteNode{.lang}.md")]
[Display(GroupName = "storage")]
public class FileWriteNode : Node
{
    public string FilePath { get; set; } = "";
    public FileWriteMode WriteMode { get; set; } = FileWriteMode.Overwrite;

    public bool AddAsNewLine { get; set; }
    public bool CreateDirectoryIfItDoesntExist { get; set; }

    public FileWriteNode()
    {
        Inputs = [new()];
        Color = "#deb887";
        Outputs = [new NodeOutput()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/file-48.png";
    }

    public enum FileWriteMode
    {
        Overwrite = 1,
        Delete = 2,
        Append = 3,
    }
}
