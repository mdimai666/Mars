using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class CallNodeHandlebarsTemplateExample : INodeExample<CallNode>
{
    public string Name => "Write template";
    public string Description => "CallNode called on render template";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new CallNode() { Name = "templateFunc1" })
            .AddNext(new TemplateNode() { Template = "<div>Template: {{Payload}}</div>" })
            .AddNext(new CallResponseNode())
            .Build();
    }
}

public class JsonNodeParseStringToObjectExample : INodeExample<JsonNode>
{
    public string Name => "Parse json string to object";
    public string Description => "Parse json string to object";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new TemplateNode()
            {
                Name = "json obj",
                Template = """
                            {
                                "name": "Dima",
                                "age": 35
                            }
                            """
            })
            .AddNext(new JsonNode())
            .AddNext(new VariableSetNode()
            {
                Setters = [new VariableSetExpression { ValuePath = "msg.Payload", Expression = "msg.Payload.age", Operation = VariableSetOperation.Set }]
            })
            .AddNext(new DebugNode())
            .Build();
    }
}
