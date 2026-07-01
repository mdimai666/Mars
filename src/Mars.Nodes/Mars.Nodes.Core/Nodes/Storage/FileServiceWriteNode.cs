using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Storage;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/FileServiceWriteNode/FileServiceWriteNode{.lang}.md")]
[Display(GroupName = "storage")]
public class FileServiceWriteNode : Node
{
    public override string TypeId => "core.FileServiceWriteNode";

    public string FilePath { get; set; } = "";
    public string FileStorageProvider { get; set; } = FileServiceReadNode.MediaProviderName;
    public bool ByFileId { get; set; }
    public string StorageFileId { get; set; } = "";
    public FileWriteNode.FileWriteMode WriteMode { get; set; } = FileWriteNode.FileWriteMode.Overwrite;

    public bool AddAsNewLine { get; set; }
    public bool CreateDirectoryIfItDoesntExist { get; set; }
    public bool CreateDatabaseRecord { get; set; } = true;

    public FileServiceWriteNode()
    {
        Inputs = [new()];
        Color = "#ffea9f";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/file-48.png";
    }

}
