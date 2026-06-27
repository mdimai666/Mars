using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using Flurl.Http;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Implements.Test.NodesForTesting;
using Mars.Nodes.Implements.Test.Services;
using NSubstitute;

namespace Mars.Nodes.Implements.Test.Nodes;

public class HttpRequestNodeTests : NodeServiceUnitTestBase
{
    private readonly MockHttpMessageHandler _httpHandler;

    public HttpRequestNodeTests()
    {
        _httpHandler = new MockHttpMessageHandler();

        var httpClient = new HttpClient(_httpHandler);
        Runtime.GetHttpClient().Returns(httpClient);
        //rns.GetConfig(Arg.Any<AuthConfig>()).Returns(new AuthConfig());
    }

    void SetupNodes(HttpRequestNode node, Action<object> callback)
    {
        var callbackNode = new TestCallBackNode() { Callback = (input, _) => callback(input.Payload!) };
        var nodes = NodesWorkflowBuilder.Create()
                .AddNext(node)
                .AddNext([callbackNode], catchAllWires: true)
                .BuildWithFlowNode();
        _nodeService.Deploy(nodes);
    }

    [Fact]
    public async Task Execute_WithGetRequest_ShouldReturnStringResponse()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/data",
            Method = "GET",
        };

        var expectedResponse = "success";
        _httpHandler.SetResponse(expectedResponse, "text/html");

        // Act
        var msg = await ExecuteNode(node);

        // Assert
        msg.Payload.Should().BeOfType<string>();
        msg.Payload.ToString().Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Execute_WithGetRequest_ShouldReturnJsonResponse()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/data",
            Method = "GET",
        };

        var expectedResponse = "{\"message\":\"success\"}";
        _httpHandler.SetResponse(expectedResponse, "application/json");

        // Act
        var msg = await ExecuteNode(node);

        // Assert
        msg.Payload.Should().BeOfType<JsonObject>();
        ((JsonObject)msg.Payload).ToJsonString().Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Execute_WithPostJsonPayload_ShouldSendJsonBody()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/create",
            Method = "POST",
        };

        var payload = new { Name = "Test", Value = 123 };
        var expectedResponse = "{\"id\":1}";
        _httpHandler.SetResponse(expectedResponse, "application/json");
        var input = new NodeMsg { Payload = payload };

        // Act
        var msg = await ExecuteNode(node, input);

        // Assert
        ((JsonObject)msg.Payload!).ToJsonString().Should().Be(expectedResponse);
        _httpHandler.LastRequestMethod.Should().Be(HttpMethod.Post);
        _httpHandler.LastRequestContent.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_WithStringPayload_ShouldSendStringBody()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/text",
            Method = "POST",
        };

        var payload = "plain text content";
        _httpHandler.SetResponse("OK", "text/plain");
        var input = new NodeMsg { Payload = payload };

        // Act
        var msg = await ExecuteNode(node, input);

        // Assert
        msg.Payload.Should().Be("OK");
        _httpHandler.LastRequestContent.Should().Contain("plain text content");
    }

    [Fact]
    public async Task Execute_WithBinaryResponse_ShouldReturnByteArray()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/file",
            Method = "GET",
        };

        var binaryData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        _httpHandler.SetResponse(binaryData, "application/octet-stream");
        var input = new NodeMsg { Payload = null };

        // Act
        var msg = await ExecuteNode(node, input);

        // Assert
        msg.Payload.Should().BeOfType<byte[]>();
        (msg.Payload as byte[]).Should().BeEquivalentTo(binaryData);
    }

    [Fact]
    public async Task Execute_WithStreamPayload_ShouldSendStreamBody()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/upload",
            Method = "POST",
        };

        var streamData = Encoding.UTF8.GetBytes("stream content");
        var payload = new MemoryStream(streamData);
        _httpHandler.SetResponse("Uploaded", "text/plain");

        var input = new NodeMsg { Payload = payload };

        // Act
        var msg = await ExecuteNode(node, input);

        // Assert
        msg.Payload.Should().Be("Uploaded");
        _httpHandler.LastRequestContent.Should().Contain("stream content");
    }

    [Fact]
    public async Task Execute_WithHttpError_ShouldHandleException()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/error",
            Method = "GET",
        };

        _httpHandler.SetErrorResponse(new HttpRequestException("Connection failed"));
        var input = new NodeMsg { Payload = null };

        // Act
        var act = () => ExecuteNode(node, input);

        // Assert
        //RNS.Received().BroadcastStatus(Arg.Is<string>(s => s == node.Id), Arg.Is<NodeStatus>(s => s.Color == "red"));
        //RNS.Received().DebugMsg(Arg.Is<string>(s => s == node.Id), Arg.Any<DebugMessage>());
        await act.Should().ThrowAsync<FlurlHttpException>();
    }

    [Fact]
    public async Task Execute_WithEmptyUrl_ShouldThrowException()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "",
            Method = "GET",
        };
        var input = new NodeMsg { Payload = null };

        // Act
        var act = () => ExecuteNode(node, input);

        // Assert
        await act.Should().ThrowAsync<NodeExecuteException>()
            .WithMessage("*Url is empty*");
    }

    [Fact]
    public async Task Execute_WithHeaders_ShouldAddHeadersToRequest()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/data",
            Method = "GET",
            Headers =
            [
                new HeaderItem { Name = "Authorization", Value = "Bearer token123" },
                new HeaderItem { Name = "X-Custom", Value = "custom-value" }
            ]
        };

        _httpHandler.SetResponse("OK", "text/plain");
        var input = new NodeMsg { Payload = null };

        // Act
        var msg = await ExecuteNode(node, input);

        // Assert
        _httpHandler.LastRequestHeaders.Should().ContainKey("Authorization");
        _httpHandler.LastRequestHeaders["Authorization"].Should().Be("Bearer token123");
        _httpHandler.LastRequestHeaders.Should().ContainKey("X-Custom");
        _httpHandler.LastRequestHeaders["X-Custom"].Should().Be("custom-value");
    }

    [Fact]
    public async Task Execute_WithPutMethod_ShouldSendPutRequest()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/update",
            Method = "PUT",
        };

        var payload = new { Id = 1, Name = "Updated" };
        _httpHandler.SetResponse("Updated", "text/plain");

        var input = new NodeMsg { Payload = payload };

        // Act
        var msg = await ExecuteNode(node, input);

        // Assert
        _httpHandler.LastRequestMethod.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task Execute_WithDeleteMethod_ShouldSendDeleteRequest()
    {
        // Arrange
        _ = nameof(HttpRequestNodeImpl.Execute);
        var node = new HttpRequestNode
        {
            Url = "https://api.example.com/delete/1",
            Method = "DELETE",
        };

        _httpHandler.SetResponse("Deleted", "text/plain");
        var input = new NodeMsg { Payload = null };

        // Act
        var msg = await ExecuteNode(node, input);

        // Assert
        _httpHandler.LastRequestMethod.Should().Be(HttpMethod.Delete);
    }

}

// Вспомогательный класс для мока HTTP ответов
public class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpResponseMessage _response = default!;
    private Exception _exception = default!;

    public HttpMethod? LastRequestMethod { get; private set; }
    public string? LastRequestContent { get; private set; }
    public Dictionary<string, string> LastRequestHeaders { get; private set; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestMethod = request.Method;
        LastRequestHeaders.Clear();

        foreach (var header in request.Headers)
        {
            LastRequestHeaders[header.Key] = string.Join(",", header.Value);
        }

        if (request.Content != null)
        {
            LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        if (_exception != null)
            throw _exception;

        return _response;
    }

    public void SetResponse(string content, string contentType)
    {
        _response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, contentType)
        };
    }

    public void SetResponse(byte[] content, string contentType)
    {
        _response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content)
        };
        _response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
    }

    public void SetErrorResponse(Exception exception)
    {
        _exception = exception;
    }
}
