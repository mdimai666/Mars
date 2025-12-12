using Flurl.Http;
using Mars.Core.Models;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class HttpRequestNodeImpl : INodeImplement<HttpRequestNode>, INodeImplement
{
    public HttpRequestNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    //-----------------------------
    public HttpRequestNodeImpl(HttpRequestNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        using var client = RED.GetHttpClient();
        var q = new FlurlClient(client);

        foreach (var head in Node.Headers.Where(s => !string.IsNullOrEmpty(s.Name)))
        {
            q.WithHeader(head.Name, head.Value);
        }

        //q.EnsureSuccessStatusCode = true;

        var method = Node.Method.ToUpper();

        try
        {
            string? response = null;
            RED.Status(new NodeStatus { Text = "request...", Color = "blue" });

            response = method switch
            {
                "GET" => await q.Request(Node.Url).GetStringAsync(),
                "POST" => await q.Request(Node.Url).PostJsonAsync(input.Payload!).ReceiveString(),
                "PUT" => await q.Request(Node.Url).PutJsonAsync(input.Payload!).ReceiveString(),
                "DELETE" => await q.Request(Node.Url).DeleteAsync().ReceiveString(),
                _ => throw new NotImplementedException()
            };

            RED.Status(new NodeStatus { Text = "200", Color = "green" });

            input.Payload = response;
            callback(input);

        }
        catch (FlurlHttpException ex)
        {
            string statusText = $" {((int)ex.StatusCode!)} {ex.Message}";
            RED.Status(NodeStatus.Error(statusText));
            RED.DebugMsg(DebugMessage.NodeMessage(Node.Id, ex.Message, MessageIntent.Warning));
        }
        catch (HttpRequestException ex)
        {
            string statusText = $" {((int)ex.StatusCode!)} {ex.StatusCode.ToString()}";
            RED.Status(NodeStatus.Error(statusText));
            RED.DebugMsg(DebugMessage.NodeMessage(Node.Id, ex.Message, MessageIntent.Warning));
        }
    }
}
