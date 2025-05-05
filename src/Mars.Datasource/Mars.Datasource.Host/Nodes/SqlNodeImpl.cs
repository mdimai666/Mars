using Mars.Datasource.Core.Nodes;
using Mars.Datasource.Host.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Host.Nodes;

public class SqlNodeImpl : INodeImplement<SqlNode>, INodeImplement
{
    public SqlNodeImpl(SqlNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public SqlNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public async Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        try
        {
            IDatasourceService ds = RED.ServiceProvider.GetRequiredService<IDatasourceService>();

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
                RED.Status(new NodeStatus { Text = "error: " + result.Message });
                RED.DebugMsg(new DebugMessage { message = result.Message, Level = Mars.Core.Models.MessageIntent.Error });
            }


            callback(input);

        }
        catch (Exception ex)
        {
            RED.Status(new NodeStatus { Text = "Exception" });
            RED.DebugMsg(ex);
        }

    }
}
