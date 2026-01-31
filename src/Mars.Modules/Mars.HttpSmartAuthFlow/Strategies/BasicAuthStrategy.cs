namespace Mars.HttpSmartAuthFlow.Strategies;

public class BasicAuthStrategy : AuthStrategyBase
{
    private readonly string _authHeader;

    public BasicAuthStrategy(AuthConfig config) : base(config)
    {
        if (string.IsNullOrEmpty(config.Username) || string.IsNullOrEmpty(config.Password))
            throw new InvalidOperationException("Username and Password are required for BasicAuth");

        var credentials = Convert.ToBase64String(
            System.Text.Encoding.ASCII.GetBytes($"{config.Username}:{config.Password}"));

        _authHeader = $"Basic {credentials}";
        _isAuthenticated = true;
    }

    public override Task ApplyAuthenticationAsync(HttpRequestMessage request)
    {
        request.Headers.Authorization =
            System.Net.Http.Headers.AuthenticationHeaderValue.Parse(_authHeader);
        return Task.CompletedTask;
    }

    public override Task<bool> HandleUnauthorizedAsync(HttpRequestMessage request)
    {
        // Basic auth не поддерживает перелогин
        return Task.FromResult(false);
    }

    public override Task InvalidateAsync()
    {
        _isAuthenticated = false;
        return Task.CompletedTask;
    }

}
