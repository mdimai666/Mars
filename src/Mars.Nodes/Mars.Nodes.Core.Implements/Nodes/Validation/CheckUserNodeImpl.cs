using Mars.Host.Shared.Interfaces;
using Mars.Nodes.Core.Nodes.Validation;
using Mars.Nodes.Host.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes.Validation;

public class CheckUserNodeImpl : INodeImplement<CheckUserNode>
{

    public CheckUserNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public CheckUserNodeImpl(CheckUserNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        var requestContext = RNS.ServiceProvider.GetRequiredService<IRequestContext>();

        bool isAuth = requestContext.IsAuthenticated;

        if (isAuth)
        {
            input.TryAdd(requestContext);
            callback(input, 0);
        }
        else
        {
            callback(input, 1);
        }

        return Task.CompletedTask;
    }
}
