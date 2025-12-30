namespace Mars.Host.Shared.Dto.Posts;

public record DeleteManyPostQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
