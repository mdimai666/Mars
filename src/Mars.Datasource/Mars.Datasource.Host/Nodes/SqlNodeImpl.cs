using Mars.Datasource.Core.Nodes;
using Mars.Datasource.Host.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Host.Nodes;

public class SqlNodeImpl : INodeImplement<SqlNode>
{
    public SqlNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public SqlNodeImpl(SqlNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        IDatasourceService ds = RNS.ServiceProvider.GetRequiredService<IDatasourceService>();

        string query = "";

        if (Node.Source == SqlNode.ESqlNodeInputSource.Static)
        {
            query = Node.SqlQuery;
        }
        else if (Node.Source == SqlNode.ESqlNodeInputSource.Payload)
        {
            ArgumentNullException.ThrowIfNull(input.Payload, nameof(input.Payload));
            query = input.Payload.ToString()!;
        }
        else
        {
            throw new NotImplementedException();
        }

        var result = await ds.SqlQuery(Node.DatasourceSlug, query);

        if (result.Ok)
        {
            input.Payload = result.Data;
        }
        else
        {
            input.Payload = null;
            RNS.Status(NodeStatus.Error("error: " + result.Message));
            RNS.DebugMsg(DebugMessage.NodeErrorMessage(Node.Id, result.Message));
        }

        callback(input);
    }
}
