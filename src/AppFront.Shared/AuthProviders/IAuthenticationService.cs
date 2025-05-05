using Mars.Shared.Contracts.Auth;

namespace AppFront.Shared.AuthProviders;

public interface IAuthenticationService
{
    Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userForRegistration);
    Task<AuthResultResponse> Login(AuthCreditionalsRequest userForAuthentication);
    Task Logout();

}
