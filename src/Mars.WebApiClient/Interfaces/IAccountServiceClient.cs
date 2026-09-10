using Mars.Identity.Contracts.Auth;

namespace Mars.WebApiClient.Interfaces;

public interface IAccountServiceClient
{
    Task<AuthResultResponse> Login(AuthCredentialsRequest authCredentials);
    Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userData);

}
