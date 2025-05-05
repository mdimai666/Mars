namespace Mars.Host.Shared.Dto.NavMenus;

public record NavMenuDetail : NavMenuSummary
{
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required IReadOnlyCollection<NavMenuItemDto> MenuItems { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required bool RolesInverse { get; init; }
}

