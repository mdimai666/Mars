using Mars.Contracts.Auth;
using Mars.Contracts.SSO;

namespace Mars.Admin.Framework.AuthProviders;

public interface IAuthenticationService
{
    Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userForRegistration);
    Task<AuthResultResponse> Login(AuthCredentialsRequest userForAuthentication);
    Task Logout();
    Task MarkUserAsAuthenticated(string token, SsoUserInfoResponse? ssoUserInfo = null);
}
