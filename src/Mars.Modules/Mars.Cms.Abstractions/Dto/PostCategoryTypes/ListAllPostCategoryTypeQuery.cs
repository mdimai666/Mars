namespace Mars.Host.Shared.Dto.PostCategoryTypes;

public record ListAllPostCategoryTypeQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
