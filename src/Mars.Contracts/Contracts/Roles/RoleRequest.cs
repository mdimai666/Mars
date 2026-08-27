using System.ComponentModel.DataAnnotations;
using Mars.Shared.Common;

namespace Mars.Shared.Contracts.Roles;

public record CreateRoleRequest
{
    [Required]
    public required string Name { get; init; }
}

public record UpdateRoleRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Name { get; init; }
}

public record ListRoleQueryRequest : BasicListQueryRequest
{
}

public record TableRoleQueryRequest : BasicTableQueryRequest
{
}
