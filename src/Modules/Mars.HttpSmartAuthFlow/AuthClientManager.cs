using System.Collections.Concurrent;
using Flurl.Http;
using Mars.HttpSmartAuthFlow.Handlers;
using Mars.HttpSmartAuthFlow.Strategies;

namespace Mars.HttpSmartAuthFlow;

public class AuthClientManager : IDisposable
{
    private readonly IAuthStrategyFactory _factory;
    private readonly ConcurrentDictionary<string, AuthClientWrapper> _clients = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastAccess = new();
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public AuthClientManager(IAuthStrategyFactory? factory = null)
    {
        _factory = factory ?? new AuthStrategyFactory();
        _cleanupTimer = new Timer(CleanupExpiredClients, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public IFlurlClient GetOrCreateClient(AuthConfig config)
    {
        var wrapper = _clients.GetOrAdd(config.Id, _ =>
        {
            var strategy = _factory.Create(config);
            var client = CreateFlurlClient(strategy);
            return new AuthClientWrapper(strategy, client);
        });

        _lastAccess[config.Id] = DateTime.UtcNow;
        return wrapper.Client;
    }

    public void InvalidateClient(string configId)
    {
        if (_clients.TryRemove(configId, out var wrapper))
        {
            if (wrapper is IDisposable wd) wd.Dispose();
            _lastAccess.TryRemove(configId, out _);
            Console.WriteLine($"[{configId}] Client invalidated and disposed");
        }
    }

    public void InvalidateAll()
    {
        foreach (var key in _clients.Keys.ToList())
        {
            InvalidateClient(key);
        }
    }

    public int GetActiveClientCount() => _clients.Count;

    public AuthConfig? GetAuthConfig(string configId) => _clients.GetValueOrDefault(configId)?.Strategy.Config;

    private IFlurlClient CreateFlurlClient(IAuthStrategy strategy)
    {
        var innerClient = new FlurlClient();

        var client = new FlurlClient(new HttpClient(
            new AuthFlowHandler(strategy)
            {
                InnerHandler = new HttpClientHandler()
            }
        ));
        return client;
    }

    private void CleanupExpiredClients(object? state)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-30); // 30 минут неактивности

        foreach (var key in _lastAccess.Keys.ToList())
        {
            if (_lastAccess.TryGetValue(key, out var lastAccess) &&
                lastAccess < cutoff &&
                _clients.TryRemove(key, out var wrapper))
            {
                if (wrapper is IDisposable wd) wd.Dispose();
                _lastAccess.TryRemove(key, out _);
                Console.WriteLine($"[{key}] Client cleaned up due to inactivity");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _cleanupTimer.Dispose();
        InvalidateAll();
    }

    private class AuthClientWrapper : IDisposable
    {
        public IAuthStrategy Strategy { get; }
        public IFlurlClient Client { get; }

        public AuthClientWrapper(IAuthStrategy strategy, IFlurlClient client)
        {
            Strategy = strategy;
            Client = client;
        }

        public void Dispose()
        {
            Client.Dispose();
            if (Strategy is IDisposable disposable)
                disposable.Dispose();
        }
    }

}
