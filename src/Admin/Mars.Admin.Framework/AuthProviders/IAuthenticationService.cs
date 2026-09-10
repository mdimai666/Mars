using Mars.Identity.Contracts.Auth;
using Mars.SSO.Contracts.Dto;

namespace Mars.Admin.Framework.AuthProviders;

public interface IAuthenticationService
{
    Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userForRegistration);
    Task<AuthResultResponse> Login(AuthCredentialsRequest userForAuthentication);
    Task Logout();
    Task MarkUserAsAuthenticated(string token, SsoUserInfoResponse? ssoUserInfo = null);
}
