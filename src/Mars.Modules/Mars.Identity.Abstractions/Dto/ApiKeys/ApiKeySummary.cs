using Mars.Core.Interfaces;

namespace Mars.Identity.Abstractions.Dto.ApiKeys;

public record ApiKeySummary : IHasId
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string KeyPrefix { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}
