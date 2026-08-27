namespace Mars.Host.Shared.Dto.PostTypes;

public record DeleteManyPostTypeQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
