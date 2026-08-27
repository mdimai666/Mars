namespace Mars.Shared.Contracts.Roles;

public class RoleDetailResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Name { get; init; }
}
