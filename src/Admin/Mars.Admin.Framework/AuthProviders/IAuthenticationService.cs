using Mars.Identity.Contracts.Auth;

namespace Mars.Admin.Framework.AuthProviders;

public interface IAuthenticationService
{
    Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userForRegistration);
    Task<AuthResultResponse> Login(AuthCredentialsRequest userForAuthentication);
    Task Logout();
}
