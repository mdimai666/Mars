namespace Mars.Shared.Contracts.NavMenus;

public class NavMenuSummaryResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
}

public class NavMenuDetailResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<NavMenuItemResponse> MenuItems { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required bool RolesInverse { get; init; }
}


public class NavMenuItemResponse
{
    public required Guid Id { get; init; }
    public required Guid ParentId { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string? Icon { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required bool RolesInverse { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required bool OpenInNewTab { get; init; }
    public required bool Disabled { get; init; }
    public required bool IsHeader { get; init; }
    public required bool IsDivider { get; init; }
}

