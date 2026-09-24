using System.ComponentModel.DataAnnotations;

namespace Mars.Identity.Contracts.Passkeys;

public record RenamePasskeyRequest
{
    [Required]
    public required string CredentialId { get; init; }

    [Required]
    [StringLength(64)]
    public required string Name { get; init; }
}
