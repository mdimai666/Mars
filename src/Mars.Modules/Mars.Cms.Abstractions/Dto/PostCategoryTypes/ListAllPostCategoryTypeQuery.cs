namespace Mars.Cms.Abstractions.Dto.PostCategoryTypes;

public record ListAllPostCategoryTypeQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
