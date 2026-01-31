using System.Text.Json;
using Flurl.Http;
using Mars.Core.Extensions;
using Mars.Core.Models;
using Mars.HttpSmartAuthFlow;
using Mars.HttpSmartAuthFlow.Strategies;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes;

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
        //CheckAuthConfigVersionAndInvalidate();
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        //using var _client = RED.GetHttpClient();
        //var client = new FlurlClient(_client);

        if (!Node.AuthConfig.Id.IsNullOrEmpty() && Node.AuthConfig.Value == null)
            throw new NodeExecuteException(Node, "Node.AuthConfig is set but Config is null");

        var isExistAuthFlow = !Node.AuthConfig.Id.IsNullOrEmpty();

        var client = isExistAuthFlow
                        ? _authClientManager.GetOrCreateClient(MapConfig())
                        : new FlurlClient(RED.GetHttpClient());

        foreach (var head in Node.Headers.Where(s => !string.IsNullOrEmpty(s.Name)))
        {
            client.WithHeader(head.Name, head.Value);
        }

        //q.EnsureSuccessStatusCode = true;

        var method = Node.Method.ToUpper();

        try
        {
            string? response = null;
            RED.Status(new NodeStatus { Text = "request...", Color = "blue" });

            response = method switch
            {
                "GET" => await client.Request(Node.Url).GetStringAsync(),
                "POST" => await client.Request(Node.Url).PostJsonAsync(input.Payload!).ReceiveString(),
                "PUT" => await client.Request(Node.Url).PutJsonAsync(input.Payload!).ReceiveString(),
                "DELETE" => await client.Request(Node.Url).DeleteAsync().ReceiveString(),
                _ => throw new NotImplementedException()
            };

            RED.Status(new NodeStatus { Text = "200", Color = "green" });

            input.Payload = response;
            callback(input);

        }
        catch (FlurlHttpException ex)
        {
            string statusText = $" {(ex.StatusCode ?? 0)} {ex.Message}";
            RED.Status(NodeStatus.Error(statusText));
            RED.DebugMsg(DebugMessage.NodeMessage(Node.Id, ex.Message, MessageIntent.Warning));
        }
        catch (HttpRequestException ex)
        {
            string statusText = $" {(ex.StatusCode ?? 0)} {ex.Message}";
            RED.Status(NodeStatus.Error(statusText));
            RED.DebugMsg(DebugMessage.NodeMessage(Node.Id, ex.Message, MessageIntent.Warning));
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

    AuthConfig? _authConfig;

    AuthConfig MapConfig()
        => _authConfig ??= new AuthConfig
        {
            Id = Node.AuthConfig.Id,
            Mode = (Mars.HttpSmartAuthFlow.AuthMode)Node.AuthConfig.Value.Mode,
            CustomType = Node.AuthConfig.Value.CustomType,
            Username = Node.AuthConfig.Value.Username,
            Password = Node.AuthConfig.Value.Password,
            TimeoutSeconds = Node.AuthConfig.Value.TimeoutSeconds,
            TokenUrl = Node.AuthConfig.Value.TokenUrl,
            ClientId = Node.AuthConfig.Value.ClientId,
            ClientSecret = Node.AuthConfig.Value.ClientSecret,
            Scope = Node.AuthConfig.Value.Scope,
            LoginPageUrl = Node.AuthConfig.Value.LoginPageUrl,
            FollowRedirects = Node.AuthConfig.Value.FollowRedirects,
            CookieEndpointConfig = new CookieEndpointConfig()
            {
                LoginEndpointUrl = Node.AuthConfig.Value.CookieEndpointConfig.LoginEndpointUrl,
                UsernameFieldName = Node.AuthConfig.Value.CookieEndpointConfig.UsernameFieldName,
                PasswordFieldName = Node.AuthConfig.Value.CookieEndpointConfig.PasswordFieldName,
                AdditionalFields = Node.AuthConfig.Value.CookieEndpointConfig.AdditionalFields,
                ContentType = (LoginEndpointContentType)Node.AuthConfig.Value.CookieEndpointConfig.ContentType,
                LoginHeaders = Node.AuthConfig.Value.CookieEndpointConfig.LoginHeaders,
                HealthCheckUrl = Node.AuthConfig.Value.CookieEndpointConfig.HealthCheckUrl,
                AuthCookieName = Node.AuthConfig.Value.CookieEndpointConfig.AuthCookieName,
                FollowRedirects = Node.AuthConfig.Value.CookieEndpointConfig.FollowRedirects,
            },
            ApiKey = Node.AuthConfig.Value.ApiKey,
            ApiKeyHeaderName = Node.AuthConfig.Value.ApiKeyHeaderName,

        };

    void CheckAuthConfigVersionAndInvalidate()
    {
        if (Node.AuthConfig.Value is not null)
        {
            var existConfig = _authClientManager.GetAuthConfig(Node.AuthConfig.Id);

            if (existConfig is not null)
            {
                var existConfigJson = JsonSerializer.Serialize(existConfig);
                var newConfigJson = JsonSerializer.Serialize(MapConfig());

                if (existConfigJson != newConfigJson)
                {
                    _authClientManager.InvalidateClient(Node.AuthConfig.Id);
                }
            }
        }

    }
}
