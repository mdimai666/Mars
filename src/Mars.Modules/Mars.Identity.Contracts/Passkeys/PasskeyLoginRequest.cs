using System.ComponentModel.DataAnnotations;

namespace Mars.Identity.Contracts.Passkeys;

public record PasskeyLoginRequest
{
    [Required]
    public required string CredentialJson { get; init; }
}
