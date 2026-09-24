using Mars.Identity.Abstractions.Dto.ApiKeys;

namespace Mars.Identity.Abstractions.Repositories;

public interface IUserApiKeyRepository
{
    Task<ApiKeyValidationDto?> GetForValidation(Guid keyId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ApiKeySummary>> ListByUser(Guid userId, CancellationToken cancellationToken);

    Task Create(CreateApiKeyQuery query, Guid id, string keyHash, string keyPrefix, CancellationToken cancellationToken);

    Task<int> CountByUser(Guid userId, CancellationToken cancellationToken);

    Task<bool> Delete(Guid id, Guid userId, CancellationToken cancellationToken);
}
