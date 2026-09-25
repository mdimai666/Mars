namespace Mars.Identity.Abstractions.Dto.ApiKeys;

public record CreateApiKeyQuery
{
    public required Guid UserId { get; init; }

    public required string Name { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}
