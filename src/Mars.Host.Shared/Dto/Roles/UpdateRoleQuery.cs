namespace Mars.Host.Shared.Dto.Roles;

public record UpdateRoleQuery
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
