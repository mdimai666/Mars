using System.Text;
using Mars.Host.Shared.Models;
using Mars.Nodes.Core.Nodes;
using Microsoft.AspNetCore.Http;

namespace Mars.Nodes.Core.Implements.Nodes;

public class HttpInNodeImpl : INodeImplement<HttpInNode>, INodeImplement
{
    public HttpInNodeImpl(HttpInNode node, IRED RED)
    {
        Node = node;
        this.RED = RED;

        HttpCatchRegister mw = new HttpCatchRegister(Node.Method, node.UrlPattern, node.Id);

        RED.RegisterHttpMiddleware(mw);
    }

    public HttpInNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public async Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        var http = input.Get<HttpInNodeHttpRequestContext>();
        //string? body = http.HttpContext.Request.Body.req
        string body = await GetRequestBody(http.HttpContext);


        input.Payload = body;

        callback(input);

        //return Task.CompletedTask;
    }

    async Task<string> GetRequestBody(HttpContext httpContext, Encoding? encoding = null)
    {
        HttpRequest request = httpContext.Request;

        //request.EnableBuffering();
        var buffer = new byte[Convert.ToInt32(request.ContentLength)];
        await request.Body.ReadAsync(buffer, 0, buffer.Length);

        string body = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);

        return body;
    }
}
