namespace Mars.Media.Abstractions.Dto.Files;

public record DeleteManyFileQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
