namespace Mars.Docker.Contracts;

public record ContainerPortBindingRequest
{
    public required int ContainerPort { get; init; }

    public required int HostPort { get; init; }

    /// <summary>tcp | udp.</summary>
    public string Protocol { get; init; } = "tcp";
}
