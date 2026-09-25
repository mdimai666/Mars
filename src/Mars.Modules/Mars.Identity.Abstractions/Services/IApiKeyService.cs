using Mars.Contracts.Common;
using Mars.Identity.Abstractions.Dto.ApiKeys;

namespace Mars.Identity.Abstractions.Services;

public interface IApiKeyService
{
    Task<UserActionResult<CreatedApiKeyDto>> Create(CreateApiKeyQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ApiKeySummary>> ListByUser(Guid userId, CancellationToken cancellationToken);

    Task<UserActionResult> Revoke(Guid keyId, Guid userId, CancellationToken cancellationToken);
}
