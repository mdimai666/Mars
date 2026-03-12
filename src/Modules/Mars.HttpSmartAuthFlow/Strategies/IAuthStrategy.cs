namespace Mars.HttpSmartAuthFlow.Strategies;

public interface IAuthStrategy
{
    AuthConfig Config { get; }
    Task ApplyAuthenticationAsync(HttpRequestMessage request);
    Task<bool> HandleUnauthorizedAsync(HttpRequestMessage request);
    Task InvalidateAsync();
}
