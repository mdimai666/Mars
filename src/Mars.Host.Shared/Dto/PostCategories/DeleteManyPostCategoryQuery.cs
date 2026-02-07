namespace Mars.Host.Shared.Dto.PostCategories;

public record DeleteManyPostCategoryQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
