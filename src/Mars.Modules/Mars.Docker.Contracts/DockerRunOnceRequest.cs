namespace Mars.Docker.Contracts;

/// <summary>
/// One-shot выполнение: создать контейнер, передать stdin/команду, собрать вывод, удалить.
/// База для нод и плагинов-рецептов (например, исполнение Python-скрипта).
/// </summary>
public record DockerRunOnceRequest
{
    /// <summary>Имя образа; при отсутствии локально будет выполнен pull.</summary>
    public required string Image { get; init; }

    public IList<string> Cmd { get; init; } = [];

    public IList<string> Env { get; init; } = [];

    public string? WorkingDir { get; init; }

    public string? User { get; init; }

    /// <summary>Содержимое stdin (например, текст скрипта).</summary>
    public string? Stdin { get; init; }

    /// <summary>Таймаут выполнения; 0/пусто — <c>DockerOptions.DefaultRunTimeoutSeconds</c>.</summary>
    public int TimeoutSeconds { get; init; }

    /// <summary>Не удалять контейнер после выполнения (для отладки).</summary>
    public bool KeepContainer { get; init; }
}
