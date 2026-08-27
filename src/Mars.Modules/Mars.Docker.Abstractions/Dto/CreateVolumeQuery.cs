namespace Mars.Docker.Host.Shared.Dto;

public record CreateVolumeQuery
{
    public required string Name { get; init; }
    public required string Driver { get; init; }
    public required IReadOnlyDictionary<string, string> DriverOpts { get; init; }
    public required IReadOnlyDictionary<string, string> Labels { get; init; }

}
