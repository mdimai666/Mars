using Mars.HttpSmartAuthFlow.Strategies;

namespace Mars.HttpSmartAuthFlow;

public class AuthStrategyFactory : IAuthStrategyFactory
{
    public virtual IAuthStrategy Create(AuthConfig config)
    {
        return config.Mode switch
        {
            AuthMode.BearerToken => new BearerTokenStrategy(config),
            AuthMode.CookieForm => new CookieFormStrategy(config),
            AuthMode.CookieEndpoint => new CookieEndpointStrategy(config),
            AuthMode.BasicAuth => new BasicAuthStrategy(config),
            AuthMode.ApiKey => new ApiKeyStrategy(config),
            _ => throw new NotSupportedException($"Auth mode {config.Mode} is not supported")
        };
    }
}
