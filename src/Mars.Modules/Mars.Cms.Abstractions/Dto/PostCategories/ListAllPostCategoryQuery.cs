namespace Mars.Host.Shared.Dto.PostCategories;

public record ListAllPostCategoryQuery
{
    public required string? Type { get; init; }
    public IReadOnlyCollection<Guid>? Ids { get; init; }
    public required string? PostTypeName { get; init; }
}
