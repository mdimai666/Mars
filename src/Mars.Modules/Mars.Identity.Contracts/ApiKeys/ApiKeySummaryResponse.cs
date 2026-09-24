namespace Mars.Identity.Contracts.ApiKeys;

public record ApiKeySummaryResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string KeyPrefix { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
