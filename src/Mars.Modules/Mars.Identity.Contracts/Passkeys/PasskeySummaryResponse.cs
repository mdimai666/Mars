namespace Mars.Identity.Contracts.Passkeys;

public record PasskeySummaryResponse
{
    public required string CredentialId { get; init; }
    public string? Name { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public bool IsBackedUp { get; init; }
    public bool IsBackupEligible { get; init; }
}
