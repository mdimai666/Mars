using Mars.Contracts.Common;
using Mars.Identity.Contracts.Passkeys;

namespace Mars.WebApiClient.Interfaces;

public interface IPasskeyServiceClient
{
    Task<IReadOnlyCollection<PasskeySummaryResponse>> List();

    Task<UserActionResult> Rename(RenamePasskeyRequest request);

    Task<UserActionResult> Delete(string credentialId);
}
