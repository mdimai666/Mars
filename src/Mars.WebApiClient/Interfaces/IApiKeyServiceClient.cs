using Mars.Contracts.Common;
using Mars.Identity.Contracts.ApiKeys;

namespace Mars.WebApiClient.Interfaces;

public interface IApiKeyServiceClient
{
    Task<IReadOnlyCollection<ApiKeySummaryResponse>> List();

    Task<UserActionResult<CreatedApiKeyResponse>> Create(CreateApiKeyRequest request);

    Task<UserActionResult> Revoke(Guid id);
}
