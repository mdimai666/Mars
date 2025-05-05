using Mars.Host.Shared.Models;
using Mars.Nodes.Core.Nodes;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mars.Nodes.Core.Implements.Nodes;

public class HttpResponseNodeImpl : INodeImplement<HttpResponseNode>, INodeImplement
{
    public HttpResponseNodeImpl(HttpResponseNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public HttpResponseNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public async Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        HttpInNodeHttpRequestContext? http = input.Get<HttpInNodeHttpRequestContext>();

        if (http == null) throw new ArgumentNullException(nameof(http) + ":HttpInNodeHttpRequestContext");

        JsonSerializerOptions opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            MaxDepth = 0,
            //IgnoreReadOnlyProperties = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        };

        string? response;

        if (input.Payload is null) response = null;
        else if (input.Payload is string)
        {
            response = input.Payload as string;
        }
        else if (input.Payload is object)
        {
            http.HttpContext.Response.ContentType = "application/json";
            response = JsonConvert.SerializeObject(input.Payload);
            //response = System.Text.Json.JsonSerializer.Serialize(input.Payload, opt);
        }
        else response = input.Payload?.ToString();

        response ??= "";

        await http.HttpContext.Response.WriteAsync(response, encoding: Encoding.UTF8); //on async body already has disposed


        //http.HttpContext.Dispose();
    }
}
