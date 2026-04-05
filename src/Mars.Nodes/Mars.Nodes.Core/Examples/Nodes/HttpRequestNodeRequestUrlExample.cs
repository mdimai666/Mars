using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class HttpRequestNodeRequestUrlExample : INodeExample<HttpRequestNode>
{
    public string Name => "Http request";
    public string Description => "Http request";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new HttpRequestNode() { Url = "http://localhost:5003/api/System/HealthCheck" })
            .AddNext(new DebugNode())
            .Build();
    }
}
