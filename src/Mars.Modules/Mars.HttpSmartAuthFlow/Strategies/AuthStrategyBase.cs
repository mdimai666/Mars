namespace Mars.HttpSmartAuthFlow.Strategies;

public abstract class AuthStrategyBase : IAuthStrategy, IDisposable
{
    protected readonly SemaphoreSlim _authLock = new(1, 1);
    protected DateTime _lastAuthTime;
    protected bool _isAuthenticated;

    public AuthConfig Config { get; }

    protected AuthStrategyBase(AuthConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public abstract Task ApplyAuthenticationAsync(HttpRequestMessage request);
    public abstract Task<bool> HandleUnauthorizedAsync(HttpRequestMessage request);
    public abstract Task InvalidateAsync();

    protected async Task ExecuteWithLockAsync(Func<Task> action)
    {
        await _authLock.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            _authLock.Release();
        }
    }

    public virtual void Dispose()
    {
        _authLock.Dispose();
    }
}
