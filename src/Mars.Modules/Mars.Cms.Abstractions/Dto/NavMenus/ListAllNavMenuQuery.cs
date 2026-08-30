namespace Mars.Cms.Abstractions.Dto.NavMenus;

public record ListAllNavMenuQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
