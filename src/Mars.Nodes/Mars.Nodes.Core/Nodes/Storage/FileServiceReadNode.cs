using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Storage;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/docs/FileServiceReadNode/FileServiceReadNode{.lang}.md")]
[Display(GroupName = "storage")]
public class FileServiceReadNode : Node, INodeOutputValueSpec
{
    public override string TypeId => "core.FileServiceReadNode";

    public string FilePath { get; set; } = "";
    public string FileStorageProvider { get; set; } = MediaProviderName;
    public bool ByFileId { get; set; }
    public string StorageFileId { get; set; } = "";

    public FileOutputMode OutputMode { get; set; }

    public FileServiceReadNode()
    {
        Inputs = [new()];
        Color = "#ffea9f";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/file-service-read.svg";
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

    public const string MediaProviderName = "Media";
    public static readonly string[] FileStorageProvidersList = [MediaProviderName];
}
