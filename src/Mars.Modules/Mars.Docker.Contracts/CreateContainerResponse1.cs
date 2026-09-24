namespace Mars.Docker.Contracts;

public class CreateContainerResponse1
{
    public required string ID { get; init; }

    public required string Name { get; init; }

    public IList<string> Warnings { get; init; } = [];
}
