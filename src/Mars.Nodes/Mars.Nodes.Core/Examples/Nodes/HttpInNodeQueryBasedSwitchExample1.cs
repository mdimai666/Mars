using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class HttpInNodeQueryBasedSwitchExample1 : INodeExample<HttpInNode>
{
    public string Name => "Query based switch";
    public string Description => "Query based switch response";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var nodes = NodesWorkflowBuilder.Create()
            .AddNext(new HttpInNode
            {
                Method = "GET",
                UrlPattern = "/example" + editorState.Nodes.Length
            })
            .AddNext(new SwitchNode
            {
                Conditions = [
                    new(){ Value = "msg.HttpInNodeHttpRequestContext.Request.Query[\"s\"]==\"777\"" },
                    new(){ Value = SwitchNode.ElseConditionValue }
                ]
            })
            .AddNext(new TemplateNode { Name = "Ok", Template = "Ok" }, new TemplateNode { Name = "Ok", Template = "No" })
            .AddNext([new HttpResponseNode()])
            .Build();

        var templates = nodes.Where(node => node is TemplateNode).ToList();
        var switchNode = nodes.First(node => node is SwitchNode);
        switchNode.Wires = [[templates[0].Id], [templates[1].Id]];

        return nodes;
    }
}
