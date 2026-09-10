using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Mars.Server.Abstractions.Services;

/// <summary>
/// Глобальный статический логгер (паттерн NLog LogManager / Serilog Log): удобный доступ
/// из кода, который не создаётся контейнером (плагины, рефлексия, catch-фолбэки).
/// <see cref="Initialize"/> вызывается ровно один раз на процесс из корня композиции:
/// продакшн — <c>MarsWebAppStartup</c>, тесты — module initializer тестовой сборки.
/// Повторные вызовы игнорируются (first-wins), чтобы несколько хостов в одном процессе
/// (тесты) не перетирали инициализацию друг друга.
/// </summary>
public static class MarsLogger
{
    private static ILoggerFactory _loggerFactory = default!;

    private static ConcurrentDictionary<Type, ILogger> loggerByType = new();

    public static void Initialize(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Interlocked.CompareExchange(ref _loggerFactory, loggerFactory, null);
    }

    public static ILogger<T> GetStaticLogger<T>()
    {
        if (Volatile.Read(ref _loggerFactory) is null)
            throw new InvalidOperationException("MarsLogger is not initialized yet.");

        return (ILogger<T>)loggerByType.GetOrAdd(typeof(T), _loggerFactory.CreateLogger<T>());
    }
}
