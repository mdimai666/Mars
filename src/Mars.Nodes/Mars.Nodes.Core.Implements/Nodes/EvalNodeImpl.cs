using DynamicExpresso;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class EvalNodeImpl : INodeImplement<EvalNode>, INodeImplement
{
    public EvalNodeImpl(EvalNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public EvalNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var ppt = VariableSetNodeImpl.CreateInterpreter(RED, input);

        var result = ppt.Get.Eval(Node.Input);

        input.Payload = result;

        callback(input);

        return Task.CompletedTask;
    }
}
