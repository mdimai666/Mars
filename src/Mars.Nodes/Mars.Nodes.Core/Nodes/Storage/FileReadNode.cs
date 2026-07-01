using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Storage;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/FileReadNode/FileReadNode{.lang}.md")]
[Display(GroupName = "storage")]
public class FileReadNode : Node
{
    public override string TypeId => "core.FileReadNode";

    public string FilePath { get; set; } = "";

    public FileOutputMode OutputMode { get; set; }

    public FileReadNode()
    {
        Inputs = [new()];
        Color = "#deb887";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/file-48.png";
    }

    public enum FileOutputMode
    {
        SingleString,
        MsgPerLine,
        SingleBuffer,
        //SingleStream
    }
}
