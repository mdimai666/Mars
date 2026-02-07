namespace Mars.Host.Shared.Dto.PostCategoryTypes;

public record DeleteManyPostCategoryTypeQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
