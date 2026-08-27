using Mars.Core.Interfaces;

namespace Mars.Host.Shared.Dto.Roles;

public record RoleSummary : IHasId
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Name { get; init; }
}
