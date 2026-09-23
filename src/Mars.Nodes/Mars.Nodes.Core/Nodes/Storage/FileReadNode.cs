using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Storage;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/FileReadNode/FileReadNode{.lang}.md")]
[Display(GroupName = "storage")]
public class FileReadNode : Node, INodeOutputValueSpec
{
    public override string TypeId => "core.FileReadNode";

    public string FilePathKind { get; set; } = InputValueKind.Const;
    public string FilePath { get; set; } = "";

    public FileOutputMode OutputMode { get; set; }

    public FileReadNode()
    {
        Inputs = [new()];
        Color = "#deb887";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/file-read.svg";
    }

    public IEnumerable<OutputValueSpec> GetOutputValueSpec()
    {
        yield return OutputMode == FileOutputMode.SingleBuffer
            ? new OutputValueSpec(nameof(NodeMsg.Payload), VarNode.ObjectTypeName, Description: "byte[]")
            : new OutputValueSpec(nameof(NodeMsg.Payload), "string",
                Description: OutputMode == FileOutputMode.MsgPerLine ? "one message per line" : null);
    }

    public enum FileOutputMode
    {
        SingleString,
        MsgPerLine,
        SingleBuffer,
        //SingleStream
    }
}
