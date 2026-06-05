using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class SplitNodeSplitStringExample : INodeExample<SplitNode>
{
    public string Name => "Split string";
    public string Description => "";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "123;456;789" })
            .AddNext(new SplitNode() { Delimiter = ";" })
            .AddNext(new DebugNode())
            .Build();
    }
}

public class SplitNodeSplitArrayExample : INodeExample<SplitNode>
{
    public string Name => "Split array";
    public string Description => "";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "123;456;789" })
            .AddNext(editorState.CreateInlineFunctionNodeById("core.InlineFunctionNode.Utils.GenerateSequentialArray")!)
            .AddNext(new SplitNode())
            .AddNext(new DebugNode())
            .Build();
    }
}
