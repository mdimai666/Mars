namespace Mars.Host.Shared.Dto.Roles;

public record CreateRoleQuery
{
    public required Guid? Id { get; init; }
    public required string Name { get; init; }

}
