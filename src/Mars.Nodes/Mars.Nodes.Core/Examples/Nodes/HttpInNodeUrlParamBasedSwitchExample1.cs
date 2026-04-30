using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class HttpInNodeUrlParamBasedSwitchExample1 : INodeExample<HttpInNode>
{
    public string Name => "Param based switch";
    public string Description => "Param based switch response";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var nodes = NodesWorkflowBuilder.Create()
            .AddNext(new HttpInNode
            {
                Method = "GET",
                UrlPattern = "/example" + editorState.Nodes.Length + "/{param1}"
            })
            .AddNext(new SwitchNode
            {
                Conditions = [
                    new(){ Value = "msg.HttpInNodeHttpRequestContext.Request.RouteValues[\"param1\"]==\"123\"" },
                    new(){ Value = SwitchNode.ElseConditionValue }
                ]
            })
            .AddNext(new TemplateNode { Name = "Ok", Template = "(Ok) param1: {{HttpInNodeHttpRequestContext.Request.RouteValues.param1}}" },
                    new TemplateNode { Name = "No", Template = "(No) param1: {{HttpInNodeHttpRequestContext.Request.RouteValues.param1}}" })
            .AddNext([new HttpResponseNode()])
            .Build();

        var templates = nodes.Where(node => node is TemplateNode).ToList();
        var switchNode = nodes.First(node => node is SwitchNode);
        switchNode.Wires = [[templates[0].Id], [templates[1].Id]];

        return nodes;
    }
}
