using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Shared.Services;

public static class MarsLogger
{
    private static ILoggerFactory _loggerFactory = default!;

    private static ConcurrentDictionary<Type, ILogger> loggerByType = new();

    public static void Initialize(ILoggerFactory loggerFactory)
    {
        if (_loggerFactory is not null) return;
            //throw new InvalidOperationException("MarsLogger already initialized!");

        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public static ILogger<T> GetStaticLogger<T>()
    {
        if (_loggerFactory is null)
            throw new InvalidOperationException("MarsLogger is not initialized yet.");

        return (ILogger<T>)loggerByType.GetOrAdd(typeof(T), _loggerFactory.CreateLogger<T>());
    }
}
