namespace Mars.Cms.Abstractions.Dto.PostCategories;

public record DeleteManyPostCategoryQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
