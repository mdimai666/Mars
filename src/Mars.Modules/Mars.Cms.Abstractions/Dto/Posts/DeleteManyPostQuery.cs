namespace Mars.Cms.Abstractions.Dto.Posts;

public record DeleteManyPostQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
