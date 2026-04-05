using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class CheckUserNodeHttpRequestCheckExample : INodeExample<CheckUserNode>
{
    public string Name => "Http request user check";
    public string Description => "Http request user check";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        int nodesCount = editorState.Nodes.Length;

        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new HttpInNode() { UrlPattern = "/url" + nodesCount })
            .AddNext(new CheckUserNode())
            .AddNext(
                NodesWorkflowBuilder.Create()
                    .AddNext(new TemplateNode() { Name = "Authorized", Template = "Authorized" })
                    .AddNext(new HttpResponseNode(), new DebugNode()),
                NodesWorkflowBuilder.Create()
                    .AddNext(new TemplateNode() { Name = "Anonim", Template = "Anonim" })
                    .AddNext(new HttpResponseNode(), new DebugNode())
            );

        var templates = builder.Nodes.Where(node => node is TemplateNode).ToList();
        builder.Nodes.First(node => node is CheckUserNode).Wires = [[templates[0].Id], [templates[1].Id]];

        return builder.Build();
    }
}
