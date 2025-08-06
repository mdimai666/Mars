namespace Mars.Host.Shared.Services;

public interface IAIToolScenarioProvidersLocator
{
    IReadOnlyCollection<string> ListProviderKeys(string[]? tags = null);
    IAIToolScenarioProvider? GetProvider(string key);
}

public interface IAIToolScenarioProvider
{
    Task<string> Handle(string promptQuery, CancellationToken cancellationToken);
}
