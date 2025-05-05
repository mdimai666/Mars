namespace Mars.Host.Services.MarsSSOClient;

// https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow#error-codes-for-authorization-endpoint-errors
public class OpenIdErrorCodesForAuthorizationEndpointErrors
{

    /// <summary>
    /// Protocol error, such as a missing required parameter.
    /// </summary>
    public const string invalid_request = "invalid_request";

    /// <summary>
    /// The client application isn't permitted to request an authorization code.
    /// </summary>
    public const string unauthorized_client = "unauthorized_client";

    /// <summary>
    /// Resource owner denied consent
    /// </summary>
    public const string access_denied = "access_denied";

    /// <summary>
    /// The authorization server doesn't support the response type in the request.
    /// </summary>
    public const string unsupported_response_type = "unsupported_response_type";

    /// <summary>
    /// The server encountered an unexpected error.
    /// </summary>
    public const string server_error = "server_error";

    /// <summary>
    /// The server is temporarily too busy to handle the request.
    /// </summary>
    public const string temporarily_unavailable = "temporarily_unavailable";

    /// <summary>
    /// The target resource is invalid because it doesn't exist, Microsoft Entra ID can't find it, or it's not correctly configured.
    /// </summary>
    public const string invalid_resource = "invalid_resource";

    /// <summary>
    /// Too many or no users found.
    /// </summary>
    public const string login_required = "login_required";

    /// <summary>
    /// The request requires user interaction.
    /// </summary>
    public const string interaction_required = "interaction_required";

}
