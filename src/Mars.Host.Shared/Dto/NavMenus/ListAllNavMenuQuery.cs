namespace Mars.Host.Shared.Dto.NavMenus;

public record ListAllNavMenuQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
