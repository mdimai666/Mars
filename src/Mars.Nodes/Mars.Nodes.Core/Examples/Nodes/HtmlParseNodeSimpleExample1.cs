using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class HtmlParseNodeSimpleExample1 : INodeExample<HtmlParseNode>
{
    public string Name => "parse Habr post titles";
    public string Description => "A simple example of parse.";

    public IReadOnlyCollection<Node> Handle()
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new HttpRequestNode
            {
                Method = "GET",
                Url = "https://habr.com"
            })
            .AddNext(new HtmlParseNode
            {
                Output = HtmlParseNodeOutput.MapToObjects,
                Selector = "article",
                InputMappings = [new HtmlParseInputMapping
                {
                    Selector = "h2",
                    ReturnValue = InputMappingReturnValue.Text,
                    OutputField = "title",
                }]

            })
            .AddNext(new DebugNode { })
            .Build();
    }
}
