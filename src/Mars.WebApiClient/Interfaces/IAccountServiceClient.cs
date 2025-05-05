using Mars.Shared.Contracts.Auth;

namespace Mars.WebApiClient.Interfaces;

public interface IAccountServiceClient
{
    Task<AuthResultResponse> Login(AuthCreditionalsRequest authCreditionals);
    Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userData);

}
