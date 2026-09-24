namespace Mars.Identity.Abstractions.Dto.Passkeys;

public record PasskeySummary
{
    public required string CredentialId { get; init; }

    public string? Name { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public bool IsBackedUp { get; init; }

    public bool IsBackupEligible { get; init; }
}
