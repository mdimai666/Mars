using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/FileWriteNode/FileWriteNode{.lang}.md")]
public class FileWriteNode : Node
{
    public string Filename { get; set; } = "";
    public FileWriteMode WriteMode { get; set; }

    public bool AddAsNewLine { get; set; }
    public bool CreateDirectoryIfItDoesntExist { get; set; }


    public FileWriteNode()
    {
        HaveInput = true;
        Color = "#deb887";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/file-48.png";
    }

    public enum FileWriteMode
    {
        Append,
        Overwrite,
        Delete
    }
}
