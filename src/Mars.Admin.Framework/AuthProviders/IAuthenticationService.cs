using Mars.Shared.Contracts.Auth;
using Mars.Shared.Contracts.SSO;

namespace AppFront.Shared.AuthProviders;

public interface IAuthenticationService
{
    Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userForRegistration);
    Task<AuthResultResponse> Login(AuthCreditionalsRequest userForAuthentication);
    Task Logout();
    Task MarkUserAsAuthenticated(string token, SsoUserInfoResponse? ssoUserInfo = null);
}
