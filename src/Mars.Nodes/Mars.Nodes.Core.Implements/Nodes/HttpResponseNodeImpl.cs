using System.Text;
using System.Text.Json;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared.HttpModule;
using Microsoft.AspNetCore.Http;

namespace Mars.Nodes.Core.Implements.Nodes;

public class HttpResponseNodeImpl : INodeImplement<HttpResponseNode>, INodeImplement
{
    public HttpResponseNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public HttpResponseNodeImpl(HttpResponseNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;
    }

    static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 0,
        //IgnoreReadOnlyProperties = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    };

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var http = input.Get<HttpInNodeHttpRequestContext>();

        if (http == null) throw new ArgumentNullException(nameof(http) + ":HttpInNodeHttpRequestContext");

        string response;

        if (input.Payload is null) response = string.Empty;
        else if (input.Payload is string str)
        {
            response = str;
            http.HttpContext.Response.ContentType = "text/html; charset=utf-8";
        }
        else if (input.Payload?.GetType().IsPrimitive == true)
        {
            response = input.Payload?.ToString() ?? string.Empty;
            http.HttpContext.Response.ContentType = "text/html; charset=utf-8";
        }
        else if (input.Payload is IFormCollection form)
        {
            http.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            response = JsonSerializer.Serialize(form.ToDictionary(x => x.Key, x => x.Value.ToString()), _jsonSerializerOptions);
        }
        else
        {
            http.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            response = JsonSerializer.Serialize(input.Payload, _jsonSerializerOptions);
        }

        if (Node.Headers.Any())
        {
            foreach (var header in Node.Headers)
            {
                http.HttpContext.Response.Headers[header.Name] = header.Value;
            }
        }

        http.HttpContext.Response.StatusCode = Node.ResponseStatusCode;
        await http.HttpContext.Response.WriteAsync(response, encoding: Encoding.UTF8, cancellationToken: parameters.CancellationToken);
    }
}
