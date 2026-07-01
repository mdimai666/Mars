using Mars.HttpSmartAuthFlow;
using Mars.HttpSmartAuthFlow.Strategies;
using Mars.Nodes.Core.Nodes.Network;

namespace Mars.Nodes.Core.Implements.Mapping;

public static class AuthFlowConfigNodeMapper
{
    public static AuthConfig ToAuthConfig(this AuthFlowConfigNode authConfig)
    {
        return new AuthConfig
        {
            Id = authConfig.Id,
            Mode = (Mars.HttpSmartAuthFlow.AuthMode)authConfig.Mode,
            CustomType = authConfig.CustomType,
            Username = authConfig.Username,
            Password = authConfig.Password,
            TimeoutSeconds = authConfig.TimeoutSeconds,
            TokenUrl = authConfig.TokenUrl,
            ClientId = authConfig.ClientId,
            ClientSecret = authConfig.ClientSecret,
            Scope = authConfig.Scope,
            LoginPageUrl = authConfig.LoginPageUrl,
            FollowRedirects = authConfig.FollowRedirects,
            CookieEndpointConfig = new CookieEndpointConfig()
            {
                LoginEndpointUrl = authConfig.CookieEndpointConfig.LoginEndpointUrl,
                UsernameFieldName = authConfig.CookieEndpointConfig.UsernameFieldName,
                PasswordFieldName = authConfig.CookieEndpointConfig.PasswordFieldName,
                AdditionalFields = authConfig.CookieEndpointConfig.AdditionalFields,
                ContentType = (LoginEndpointContentType)authConfig.CookieEndpointConfig.ContentType,
                LoginHeaders = authConfig.CookieEndpointConfig.LoginHeaders,
                HealthCheckUrl = authConfig.CookieEndpointConfig.HealthCheckUrl,
                AuthCookieName = authConfig.CookieEndpointConfig.AuthCookieName,
                FollowRedirects = authConfig.CookieEndpointConfig.FollowRedirects,
            },
            ApiKey = authConfig.ApiKey,
            ApiKeyHeaderName = authConfig.ApiKeyHeaderName,

        };
    }
}
