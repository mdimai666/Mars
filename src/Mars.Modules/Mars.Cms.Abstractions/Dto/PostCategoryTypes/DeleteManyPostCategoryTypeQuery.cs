namespace Mars.Cms.Abstractions.Dto.PostCategoryTypes;

public record DeleteManyPostCategoryTypeQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
