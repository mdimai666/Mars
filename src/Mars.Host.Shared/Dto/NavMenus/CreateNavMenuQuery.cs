namespace Mars.Host.Shared.Dto.NavMenus;

public record CreateNavMenuQuery
{
    public required Guid? Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required IReadOnlyCollection<NavMenuItemDto> MenuItems { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required bool RolesInverse { get; init; }
}
