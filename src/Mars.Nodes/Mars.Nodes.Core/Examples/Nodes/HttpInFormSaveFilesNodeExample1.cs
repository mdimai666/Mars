using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Nodes.Network;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class HttpInFormSaveFilesNodeExample1 : INodeExample<HttpInFormSaveFilesNode>
{
    public string Name => "Write http file to media";
    public string Description => "";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new HttpInNode
            {
                Method = "POST",
                UrlPattern = "/accept_file" + editorState.Nodes.Length,
                AllowMultipart = true,
                MaxFileSize = "10mb"
            })
            .AddNext(new HttpInFormSaveFilesNode
            {
                SaveInMediaFiles = true,
                AllowSaveFileOutsideUploads = false
            })
            .AddNext(new HttpResponseNode(), new DebugNode())
            .Build();
    }
}
