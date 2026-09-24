namespace Mars.Docker.Contracts;

public class ContainerLogsResponse1
{
    public string Stdout { get; init; } = string.Empty;

    public string Stderr { get; init; } = string.Empty;
}
