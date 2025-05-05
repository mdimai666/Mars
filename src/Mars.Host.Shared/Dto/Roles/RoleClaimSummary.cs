namespace Mars.Host.Shared.Dto.Roles;

public record RoleClaimSummary
{
    public required int Id { get; init; }
    public required Guid RoleId { get; init; }
    public required string ClaimType { get; init; }
    public required string ClaimValue { get; init; }

}
