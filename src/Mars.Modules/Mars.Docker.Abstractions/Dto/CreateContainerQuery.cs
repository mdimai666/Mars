using Mars.Docker.Contracts;

namespace Mars.Docker.Abstractions.Dto;

public record CreateContainerQuery
{
    public required string Image { get; init; }

    public string? Name { get; init; }

    public IList<string> Cmd { get; init; } = [];

    public IList<string> Env { get; init; } = [];

    public IList<ContainerPortBindingRequest> Ports { get; init; } = [];

    public string? WorkingDir { get; init; }

    public string? RestartPolicy { get; init; }

    public bool AutoRemove { get; init; }
}
