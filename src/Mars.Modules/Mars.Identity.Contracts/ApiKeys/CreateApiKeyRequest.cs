using System.ComponentModel.DataAnnotations;

namespace Mars.Identity.Contracts.ApiKeys;

public record CreateApiKeyRequest
{
    [Required]
    [StringLength(256)]
    public required string Name { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}
