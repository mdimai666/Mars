namespace Mars.Host.Shared.Dto.Files;

public record UpdateFileQuery
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required FileEntityMetaDto? Meta { get; init; }
}
