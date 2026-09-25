namespace Mars.Docker.Contracts;

/// <summary>
/// Простой запрос создания контейнера (без дисков/сетей — редактор намеренно минимальный).
/// </summary>
public record CreateContainerRequest
{
    /// <summary>Имя образа (при отсутствии локально будет выполнен pull).</summary>
    public required string Image { get; init; }

    /// <summary>Имя контейнера; пустое — docker назначит сам.</summary>
    public string? Name { get; init; }

    /// <summary>Команда (CMD), например ["echo", "hi"].</summary>
    public IList<string> Cmd { get; init; } = [];

    /// <summary>Переменные окружения в формате KEY=VALUE.</summary>
    public IList<string> Env { get; init; } = [];

    /// <summary>Публикация портов host → container.</summary>
    public IList<ContainerPortBindingRequest> Ports { get; init; } = [];

    public string? WorkingDir { get; init; }

    /// <summary>no | always | unless-stopped | on-failure.</summary>
    public string? RestartPolicy { get; init; }

    /// <summary>Удалить контейнер после остановки.</summary>
    public bool AutoRemove { get; init; }
}
