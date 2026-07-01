using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Functions;

public class EvalNodeImpl : INodeImplement<EvalNode>
{
    public EvalNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public EvalNodeImpl(EvalNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var ppt = VariableSetNodeImpl.CreateInterpreter(RNS, input);

        var result = ppt.Get.Eval(Node.Input);

        input.Payload = result;

        callback(input);

        return Task.CompletedTask;
    }
}
