using System.Text;
using System.Text.Json;
using Flurl.Http;
using Mars.Core.Extensions;
using Mars.HttpSmartAuthFlow;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements.Mapping;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Nodes;
using static Mars.Nodes.Core.Nodes.HttpRequestNode;
using JsonNode = System.Text.Json.Nodes.JsonNode;

namespace Mars.Nodes.Core.Implements.Nodes;

public class HttpRequestNodeImpl : INodeImplement<HttpRequestNode>, INodeImplement
{
    private readonly AuthClientManager _authClientManager;

    public HttpRequestNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    //-----------------------------
    public HttpRequestNodeImpl(HttpRequestNode node, IRED red, AuthClientManager authClientManager)
    {
        Node = node;
        RED = red;
        _authClientManager = authClientManager;
        Node.AuthConfig = RED.GetConfig(node.AuthConfig);
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (!Node.AuthConfig.Id.IsNullOrEmpty() && Node.AuthConfig.Value == null)
            throw new NodeExecuteException(Node, "Node.AuthConfig is set but Config is null");

        var isExistAuthFlow = !Node.AuthConfig.Id.IsNullOrEmpty();

        var client = isExistAuthFlow
            ? _authClientManager.GetOrCreateClient(MapConfig())
            : new FlurlClient(RED.GetHttpClient());

        var ppt = VariableSetNodeImpl.CreateInterpreter(RED, input);

        var method = Node.Method?.Trim().ToUpperInvariant() ?? "GET";
        var requestUrl = VariableSetNodeImpl.ReadFieldAsExpression(Node.Url, ppt);

        if (string.IsNullOrEmpty(requestUrl))
            throw new NodeExecuteException(Node, "Url is empty");

        try
        {
            RED.Status(new NodeStatus { Text = "request...", Color = "blue" });

            var request = client.Request(requestUrl);

            // Подготовка тела запроса в зависимости от типа Payload
            var content = PrepareRequestBody(input.Payload);

            foreach (var head in Node.Headers.Where(s => !string.IsNullOrEmpty(s.Name)))
            {
                client.WithHeader(head.Name, head.Value);
            }

            IFlurlResponse response = await SendRequestAsync(request, method, content, parameters.CancellationToken);

            // Устанавливаем статус по коду ответа
            bool isSuccess = response.StatusCode >= 200 && response.StatusCode <= 299;
            RED.Status(new NodeStatus { Text = response.StatusCode.ToString(), Color = isSuccess ? "green" : "red" });

            var responseStream = await response.GetStreamAsync();
            var contentType = response.ResponseMessage.Content.Headers.ContentType?.MediaType;

            // Преобразуем Stream в нужный тип на основе Content-Type и/или настроек ноды
            var payload = await ConvertResponseStreamAsync(responseStream, contentType, Node.ReturnResponse, parameters.CancellationToken);
            input.Payload = payload;

            var requestInfo = new HttpRequestInfo(request, response, Node.ReturnResponse);
            input.Set(requestInfo);

            callback(input);
        }
        catch (FlurlHttpException ex)
        {
            string statusText = $"{ex.StatusCode} {ex.Message}";
            RED.Status(NodeStatus.Error(statusText));
            throw;
        }
        catch (HttpRequestException ex)
        {
            string statusText = $"{ex.StatusCode} {ex.Message}";
            RED.Status(NodeStatus.Error(statusText));
            throw;
        }
        finally
        {
            if (!isExistAuthFlow)
            {
                client.HttpClient?.Dispose();
                client?.Dispose();
            }
        }
    }

    private HttpContent? PrepareRequestBody(object? payload)
    {
        if (payload == null) return null;

        return payload switch
        {
            string s => new StringContent(s, Encoding.UTF8, "text/plain"),
            byte[] b => new ByteArrayContent(b),
            Stream stream => new StreamContent(stream),
            _ => new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private Task<IFlurlResponse> SendRequestAsync(IFlurlRequest request, string method, HttpContent? content, CancellationToken ct)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => request.GetAsync(cancellationToken: ct),
            "POST" => request.PostAsync(content, cancellationToken: ct),
            "PUT" => request.PutAsync(content, cancellationToken: ct),
            "PATCH" => request.PatchAsync(content, cancellationToken: ct),
            "DELETE" => request.SendAsync(HttpMethod.Delete, content, cancellationToken: ct),
            "HEAD" => request.HeadAsync(cancellationToken: ct),
            "OPTIONS" => request.OptionsAsync(cancellationToken: ct),
            _ => request.SendAsync(new HttpMethod(method), content, cancellationToken: ct)
        };
    }

    private async Task<object> ConvertResponseStreamAsync(Stream stream, string? contentType, ReturnResponseType returnResponseType, CancellationToken ct)
    {
        if (returnResponseType != ReturnResponseType.Auto)
        {
            return returnResponseType switch
            {
                ReturnResponseType.Stream => stream,
                ReturnResponseType.String => await ReadStreamAsStringAsync(stream, ct),
                ReturnResponseType.Bytes => await ReadStreamAsBytesAsync(stream, ct),
                ReturnResponseType.Object => await DeserializeJsonFromStreamAsync(stream, ct),
                _ => throw new NotImplementedException($"ReturnResponseType '{returnResponseType}' not implemented")
            };
        }

        // Если нужно вернуть как Stream — просто возвращаем его
        // Можно добавить флаг в Node, например: Node.ReturnAsStream
        // Пока предположим, что если Content-Type binary/octet-stream — возвращаем байты или Stream
        if (contentType == null)
            contentType = "application/octet-stream";

        return contentType.ToLowerInvariant() switch
        {
            "application/json" => await DeserializeJsonFromStreamAsync(stream, ct),
            "text/plain" or "text/html" or "application/xml" => await ReadStreamAsStringAsync(stream, ct),
            "application/octet-stream" or _ when IsBinaryContentType(contentType) => await ReadStreamAsBytesAsync(stream, ct),
            _ => await ReadStreamAsStringAsync(stream, ct) // fallback
        };
    }

    private bool IsBinaryContentType(string contentType)
    {
        return contentType.Contains("image/") ||
               contentType.Contains("audio/") ||
               contentType.Contains("video/") ||
               contentType.Contains("application/pdf") ||
               contentType.Contains("application/octet-stream") ||
               contentType.Contains("application/zip");
    }

    private async Task<string> ReadStreamAsStringAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(ct);
    }

    private async Task<byte[]> ReadStreamAsBytesAsync(Stream stream, CancellationToken ct)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        return memoryStream.ToArray();
    }

    private async Task<object> DeserializeJsonFromStreamAsync(Stream stream, CancellationToken ct)
    {
        var jsonNode = await JsonNode.ParseAsync(stream, cancellationToken: ct);
        return jsonNode ?? new object();
    }

    AuthConfig? _authConfig;
    AuthConfig MapConfig() => _authConfig ??= Node.AuthConfig.Value!.ToAuthConfig();

}
