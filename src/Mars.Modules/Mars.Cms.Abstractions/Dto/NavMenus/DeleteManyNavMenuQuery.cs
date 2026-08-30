namespace Mars.Cms.Abstractions.Dto.NavMenus;

public record DeleteManyNavMenuQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
