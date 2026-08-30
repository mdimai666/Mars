namespace Mars.Cms.Abstractions.Dto.PostTypes;

public record DeleteManyPostTypeQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
