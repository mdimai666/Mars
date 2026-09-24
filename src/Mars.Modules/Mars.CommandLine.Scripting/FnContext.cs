using Microsoft.Extensions.DependencyInjection;

namespace Mars.CommandLine.Scripting;

/// <summary>
/// Globals-объект скриптов `mars fn`: члены доступны в коде без квалификации.
/// DI живого инстанса, аргументы командной строки, отмена (Ctrl+C).
/// </summary>
public class FnContext(IServiceProvider services, string[] args, CancellationToken cancellationToken)
{
    /// <summary>Корневой провайдер сервисов приложения.</summary>
    public IServiceProvider Services { get; } = services;

    /// <summary>Аргументы скрипта (всё, что передано после имени команды/опций).</summary>
    public string[] Args { get; } = args;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public T? GetService<T>() where T : class => Services.GetService<T>();

    public T GetRequiredService<T>() where T : notnull => Services.GetRequiredService<T>();

    /// <summary>Скоуп для scoped-сервисов (используй через using — скрипт одноразовый).</summary>
    public IServiceScope CreateScope() => Services.CreateScope();
}
