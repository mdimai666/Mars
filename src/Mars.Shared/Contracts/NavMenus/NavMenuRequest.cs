using System.ComponentModel.DataAnnotations;
using Mars.Shared.Common;

namespace Mars.Shared.Contracts.NavMenus;

public record CreateNavMenuRequest
{
    public required Guid? Id { get; init; }
    [Required]
    public required string Title { get; init; }
    [Required]
    public required string Slug { get; init; }
    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required IReadOnlyCollection<CreateNavMenuItemRequest> MenuItems { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required bool RolesInverse { get; init; }
}

public record UpdateNavMenuRequest
{
    public required Guid Id { get; init; }
    [Required]
    public required string Title { get; init; }
    [Required]
    public required string Slug { get; init; }
    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required IReadOnlyCollection<UpdateNavMenuItemRequest> MenuItems { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required bool RolesInverse { get; init; }
}

public record CreateNavMenuItemRequest
{
    public required Guid Id { get; init; } = Guid.NewGuid();
    public required Guid ParentId { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string? Icon { get; init; }
    public required List<string> Roles { get; init; } = new();
    public required bool RolesInverse { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required bool OpenInNewTab { get; init; }
    public required bool Disabled { get; init; }
    public required bool IsHeader { get; init; }
    public required bool IsDivider { get; init; }
}

public record UpdateNavMenuItemRequest
{
    public required Guid Id { get; init; } = Guid.NewGuid();
    public required Guid ParentId { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string? Icon { get; init; }
    public required List<string> Roles { get; init; } = new();
    public required bool RolesInverse { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required bool OpenInNewTab { get; init; }
    public required bool Disabled { get; init; }
    public required bool IsHeader { get; init; }
    public required bool IsDivider { get; init; }
}

public record ListNavMenuQueryRequest : BasicListQueryRequest
{
}

public record TableNavMenuQueryRequest : BasicTableQueryRequest
{
}
