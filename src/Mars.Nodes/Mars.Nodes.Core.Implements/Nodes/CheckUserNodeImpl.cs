using Mars.Host.Shared.Interfaces;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class CheckUserNodeImpl : INodeImplement<CheckUserNode>, INodeImplement
{

    public CheckUserNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public CheckUserNodeImpl(CheckUserNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        var requestContext = RED.ServiceProvider.GetRequiredService<IRequestContext>();

        bool isAuth = requestContext.IsAuthenticated;

        if (isAuth)
        {
            input.Add(requestContext);
            callback(input, 0);
        }
        else
        {
            callback(input, 1);
        }

        return Task.CompletedTask;
    }
}
