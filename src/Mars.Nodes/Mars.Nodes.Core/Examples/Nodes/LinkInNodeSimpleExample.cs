using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class LinkInNodeSimpleExample : INodeExample<LinkInNode>
{
    public string Name => "Simple link";
    public string Description => "Simple link";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var outNode1 = Guid.NewGuid().ToString();

        var builder = NodesWorkflowBuilder.Create().AddNext(
                        NodesWorkflowBuilder.Create()
                            .AddNext(new InjectNode())
                            .AddNext(new LinkInNode() { OutLinksIds = [outNode1] }),
                        NodesWorkflowBuilder.Create()
                            .AddNext(new LinkOutNode() { Id = outNode1 })
                            .AddNext(new DebugNode())
                        );

        return builder.Build();
    }
}

public class LinkOutNodeSimpleExample : INodeExample<LinkOutNode>
{
    public string Name => "Simple link";
    public string Description => "Simple link";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return new LinkInNodeSimpleExample().Handle(editorState);
    }
}
