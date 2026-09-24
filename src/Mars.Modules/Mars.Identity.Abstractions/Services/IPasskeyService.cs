using Mars.Contracts.Common;
using Mars.Identity.Abstractions.Dto.Auth;
using Mars.Identity.Abstractions.Dto.Passkeys;

namespace Mars.Identity.Abstractions.Services;

public interface IPasskeyService
{
    Task<IReadOnlyCollection<PasskeySummary>> List(Guid userId);

    Task<string?> BeginRegister(Guid userId);

    Task<UserActionResult> CompleteRegister(Guid userId, string credentialJson, string? name);

    Task<UserActionResult> Rename(Guid userId, string credentialId, string name);

    Task<UserActionResult> Delete(Guid userId, string credentialId);

    Task<string> BeginLogin();

    Task<AuthResultDto> Login(string credentialJson, CancellationToken cancellationToken);
}
