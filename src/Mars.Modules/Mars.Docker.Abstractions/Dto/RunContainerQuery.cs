namespace Mars.Docker.Abstractions.Dto;

public record RunContainerQuery
{
    public required string Image { get; init; }

    public IList<string> Cmd { get; init; } = [];

    public IList<string> Env { get; init; } = [];

    public string? WorkingDir { get; init; }

    public string? User { get; init; }

    public string? Stdin { get; init; }

    public int TimeoutSeconds { get; init; }

    public bool KeepContainer { get; init; }
}
