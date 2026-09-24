using Mars.Identity.Abstractions.Dto.ApiKeys;
using Mars.Identity.Contracts.ApiKeys;

namespace Mars.Identity.Abstractions.Mappings.ApiKeys;

public static class ApiKeyMapping
{
    public static ApiKeySummaryResponse ToResponse(this ApiKeySummary dto)
        => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            KeyPrefix = dto.KeyPrefix,
            CreatedAt = dto.CreatedAt,
            ExpiresAt = dto.ExpiresAt,
        };

    public static CreatedApiKeyResponse ToResponse(this CreatedApiKeyDto dto)
        => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            KeyPrefix = dto.KeyPrefix,
            CreatedAt = dto.CreatedAt,
            ExpiresAt = dto.ExpiresAt,
            Key = dto.Key,
        };

    public static CreateApiKeyQuery ToQuery(this CreateApiKeyRequest request, Guid userId)
        => new()
        {
            UserId = userId,
            Name = request.Name,
            ExpiresAt = request.ExpiresAt,
        };
}
