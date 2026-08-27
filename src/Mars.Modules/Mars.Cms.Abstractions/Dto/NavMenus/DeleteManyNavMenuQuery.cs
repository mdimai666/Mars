namespace Mars.Host.Shared.Dto.NavMenus;

public record DeleteManyNavMenuQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
