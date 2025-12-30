namespace Mars.Host.Shared.Dto.Files;

public record DeleteManyFileQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
