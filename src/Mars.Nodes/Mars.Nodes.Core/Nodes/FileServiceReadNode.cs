using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/FileServiceReadNode/FileServiceReadNode{.lang}.md")]
[Display(GroupName = "storage")]
public class FileServiceReadNode : Node
{
    public string FilePath { get; set; } = "";
    public string FileStorageProvider { get; set; } = MediaProviderName;
    public bool ByFileId { get; set; }
    public string StorageFileId { get; set; } = "";

    public FileOutputMode OutputMode { get; set; }

    public FileServiceReadNode()
    {
        Inputs = [new()];
        Color = "#ffea9f";
        Outputs = [new NodeOutput()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/file-48.png";
    }

    public enum FileOutputMode
    {
        SingleString,
        MsgPerLine,
        SingleBuffer,
    }
    public const string MediaProviderName = "Media";
    public static readonly string[] FileStorageProvidersList = [MediaProviderName];
}
