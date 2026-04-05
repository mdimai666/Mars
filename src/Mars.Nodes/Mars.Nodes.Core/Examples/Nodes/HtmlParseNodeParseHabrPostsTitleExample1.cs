using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class HtmlParseNodeParseHabrPostsTitleExample1 : INodeExample<HtmlParseNode>
{
    public string Name => "Parse Habr post titles";
    public string Description => "A simple example of parse.";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
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
