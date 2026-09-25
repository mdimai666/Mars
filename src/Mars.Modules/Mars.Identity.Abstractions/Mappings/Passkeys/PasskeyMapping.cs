using Mars.Identity.Abstractions.Dto.Passkeys;
using Mars.Identity.Contracts.Passkeys;

namespace Mars.Identity.Abstractions.Mappings.Passkeys;

public static class PasskeyMapping
{
    public static PasskeySummaryResponse ToResponse(this PasskeySummary dto)
        => new()
        {
            CredentialId = dto.CredentialId,
            Name = dto.Name,
            CreatedAt = dto.CreatedAt,
            IsBackedUp = dto.IsBackedUp,
            IsBackupEligible = dto.IsBackupEligible,
        };
}
