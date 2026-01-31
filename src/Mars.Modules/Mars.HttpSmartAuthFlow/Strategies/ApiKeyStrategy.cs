namespace Mars.HttpSmartAuthFlow.Strategies;

public class ApiKeyStrategy : AuthStrategyBase
{
    private readonly string _apiKey;
    private readonly string _headerName;

    public ApiKeyStrategy(AuthConfig config) : base(config)
    {
        _apiKey = config.ApiKey ?? throw new InvalidOperationException("ApiKey is required");
        _headerName = config.ApiKeyHeaderName ?? "X-API-Key";
        _isAuthenticated = true;
    }

    public override Task ApplyAuthenticationAsync(HttpRequestMessage request)
    {
        request.Headers.Add(_headerName, _apiKey);
        return Task.CompletedTask;
    }

    public override Task<bool> HandleUnauthorizedAsync(HttpRequestMessage request)
    {
        // API Key не поддерживает перелогин
        return Task.FromResult(false);
    }

    public override Task InvalidateAsync()
    {
        _isAuthenticated = false;
        return Task.CompletedTask;
    }

}
