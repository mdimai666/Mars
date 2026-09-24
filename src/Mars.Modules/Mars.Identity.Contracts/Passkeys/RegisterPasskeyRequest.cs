using System.ComponentModel.DataAnnotations;

namespace Mars.Identity.Contracts.Passkeys;

public record RegisterPasskeyRequest
{
    [Required]
    public required string CredentialJson { get; init; }

    [StringLength(64)]
    public string? Name { get; init; }
}
