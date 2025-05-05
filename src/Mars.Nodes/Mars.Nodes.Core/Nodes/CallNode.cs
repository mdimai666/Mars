using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/CallNode/CallNode{.lang}.md")]
public class CallNode : Node
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);

    public CallNode()
    {
        haveInput = false;
        Color = "#7a78fe";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/NodeWorkspace/nodes/chunk-48.png";
    }

    public class CallNodeCallbackAction
    {
        public Action<object?> callback;
        public string nodeId;

        public CallNodeCallbackAction(Action<object?> callback, string nodeId)
        {
            this.callback = callback;
            this.nodeId = nodeId;
        }
    }
}

[FunctionApiDocument("./_content/NodeFormEditor/Docs/CallResponseNode/CallResponseNode{.lang}.md")]
public class CallResponseNode : Node
{
    public CallResponseNode()
    {
        isInjectable = false;
        Color = "#7a78fe";
        this.haveInput = true;
        Icon = "_content/NodeWorkspace/nodes/chunk-48.png";
    }
}
