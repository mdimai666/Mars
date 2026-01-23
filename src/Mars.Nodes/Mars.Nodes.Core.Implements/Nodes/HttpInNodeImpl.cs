using Mars.Nodes.Core.Implements.Extensions;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared.HttpModule;

namespace Mars.Nodes.Core.Implements.Nodes;

public class HttpInNodeImpl : INodeImplement<HttpInNode>, INodeImplement
{
    public HttpInNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public HttpInNodeImpl(HttpInNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;

        var mw = new HttpCatchRegister(Node.Method, node.UrlPattern, node.Id);

        _RED.RegisterHttpMiddleware(mw);
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var http = input.Get<HttpInNodeHttpRequestContext>();
        //string? body = http.HttpContext.Request.Body.req
        string body = await http.HttpContext.GetRequestBodyAsStringAsync();

        input.Payload = body;

        callback(input);
    }

}
