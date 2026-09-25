namespace Mars.Docker.Contracts;

/// <summary>Выполнение команды (CLI) в запущенном контейнере.</summary>
public record DockerExecRequest
{
    public required IList<string> Cmd { get; init; }

    public IList<string> Env { get; init; } = [];

    public string? WorkingDir { get; init; }

    public string? User { get; init; }

    /// <summary>Содержимое stdin.</summary>
    public string? Stdin { get; init; }

    /// <summary>Таймаут выполнения; 0/пусто — <c>DockerOptions.DefaultRunTimeoutSeconds</c>.</summary>
    public int TimeoutSeconds { get; init; }
}
