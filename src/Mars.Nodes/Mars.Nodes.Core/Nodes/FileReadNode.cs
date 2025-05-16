using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/FileReadNode/FileReadNode{.lang}.md")]
public class FileReadNode : Node
{
    public string Filename { get; set; } = "";
    public FileOutputMode OutputMode { get; set; }

    public FileReadNode()
    {
        HaveInput = true;
        Color = "#deb887";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/file-48.png";
    }

    public enum FileOutputMode
    {
        SingleString,
        MsgPerLine,
        SingleBuffer,
    }
}
