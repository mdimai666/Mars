using System.Net;
using Flurl.Http;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Shared.Contracts.Auth;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class AccountServiceClient : BasicServiceClient, IAccountServiceClient
{
    public AccountServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Account";
    }

    public Task<AuthResultResponse> Login(AuthCreditionalsRequest authCreditionals)
        => _client.Request($"{_basePath}{_controllerName}", "Login")
                    .AllowAnyHttpStatus()
                    .WithOAuthBearerToken("")
                    .PostJsonAsync(authCreditionals)
                    .ReceiveJson<AuthResultResponse>();

    public async Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userData)
    {
        try
        {
            var res = await _client.Request($"{_basePath}{_controllerName}", "RegisterUser")
                        .AllowAnyHttpStatus()
                        .WithOAuthBearerToken("")
                        .PostJsonAsync(userData);

            return await res.GetJsonAsync<RegistrationResultResponse>();
        }
        catch (MarsValidationException ex)
        {
            return new RegistrationResultResponse
            {
                IsSuccessfulRegistration = false,
                Errors = ex.Errors.Values.SelectMany(s => s).ToList(),
                Code = (int)HttpStatusCode.BadRequest,
            };
        }
        catch (UserActionException ex)
        {
            return new RegistrationResultResponse
            {
                IsSuccessfulRegistration = false,
                Errors = [ex.Message],
                Code = HttpConstants.UserActionErrorCode466
            };
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == (int)HttpStatusCode.InternalServerError)
        {
            return new RegistrationResultResponse
            {
                IsSuccessfulRegistration = false,
                Errors = [ex.Message],
                Code = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}
