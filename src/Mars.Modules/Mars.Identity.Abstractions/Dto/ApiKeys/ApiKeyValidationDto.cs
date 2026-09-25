namespace Mars.Identity.Abstractions.Dto.ApiKeys;

public record ApiKeyValidationDto
{
    public required Guid UserId { get; init; }

    public required string KeyHash { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}
