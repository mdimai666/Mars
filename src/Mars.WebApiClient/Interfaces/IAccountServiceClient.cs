using Mars.Identity.Contracts.Auth;

namespace Mars.WebApiClient.Interfaces;

public interface IAccountServiceClient
{
    Task<AuthResultResponse> Login(AuthCredentialsRequest authCredentials);
    Task Logout();
    Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userData);

}
