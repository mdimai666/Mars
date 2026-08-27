namespace Mars.Identity.Abstractions.Dto.Roles;

public record UpdateRoleQuery
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
