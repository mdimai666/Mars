using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/AuthFlowConfigNode/AuthFlowConfigNode{.lang}.md")]
[Display(GroupName = "network")]
public class AuthFlowConfigNode : ConfigNode
{
    public AuthFlowNodeMode Mode { get; set; } = AuthFlowNodeMode.BearerToken;
    public string? CustomType { get; set; }

    // Common
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;

    // Bearer Token
    public string? TokenUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string Scope { get; set; } = "openid email";

    // Cookie Form
    public string? LoginPageUrl { get; set; }
    public bool FollowRedirects { get; set; } = true;

    // Cookie Endpoint (через прямой API вызов)
    public AuthFlowCookieEndpointConfig CookieEndpointConfig { get; set; } = new();

    // API Key
    public string? ApiKey { get; set; }
    public string? ApiKeyHeaderName { get; set; } = "X-API-Key";
}

public enum AuthFlowNodeMode
{
    BearerToken,
    CookieForm,        // Через парсинг HTML формы
    CookieEndpoint,    // Через прямой API эндпоинт
    BasicAuth,
    ApiKey
}

/// <summary>
/// see CookieEndpointConfig
/// </summary>
public class AuthFlowCookieEndpointConfig
{
    public string LoginEndpointUrl { get; set; } = string.Empty;
    public string UsernameFieldName { get; set; } = "username";
    public string PasswordFieldName { get; set; } = "password";
    public Dictionary<string, string> AdditionalFields { get; set; } = [];
    public AuthFlowLoginEndpointContentType ContentType { get; set; } = AuthFlowLoginEndpointContentType.FormData;
    public Dictionary<string, string> LoginHeaders { get; set; } = [];
    public string? HealthCheckUrl { get; set; }
    public string? AuthCookieName { get; set; }
    public bool FollowRedirects { get; set; } = true;

}

public enum AuthFlowLoginEndpointContentType
{
    /// <summary>
    /// application/x-www-form-urlencoded (по умолчанию)
    /// </summary>
    FormData,

    /// <summary>
    /// application/json
    /// </summary>
    Json,

    /// <summary>
    /// multipart/form-data
    /// </summary>
    Multipart
}
