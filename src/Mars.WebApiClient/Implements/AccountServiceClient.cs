using System.Net;
using Flurl.Http;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Identity.Contracts.Auth;
using Mars.Identity.Contracts.Users;
using Mars.Identity.Contracts.Users.UserProfiles;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class AccountServiceClient : BasicServiceClient, IAccountServiceClient
{
    public AccountServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Account";
    }

    public Task<AuthResultResponse> Login(AuthCredentialsRequest authCredentials)
        => _client.Request($"{_basePath}{_controllerName}", "Login")
                    .AllowAnyHttpStatus()
                    .WithOAuthBearerToken("")
                    .PostJsonAsync(authCredentials)
                    .ReceiveJson<AuthResultResponse>();

    public async Task Logout()
        => await _client.Request($"{_basePath}{_controllerName}", "Logout")
                    .AllowAnyHttpStatus()
                    .PostAsync();

    public Task<UserProfileEditViewModel> GetProfileEditModel()
        => _client.Request($"{_basePath}{_controllerName}", "Profile", "EditModel")
                    .OnError(OnStatus404ThrowException)
                    .GetJsonAsync<UserProfileEditViewModel>();

    public Task<UserDetailResponse> UpdateProfile(UpdateUserProfileRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "Profile")
                    .PutJsonAsync(request)
                    .ReceiveJson<UserDetailResponse>();

    public Task<AvatarUploadResponse> UploadAvatar(Stream image, string fileName, Guid? userId = null)
    {
        var request = _client.Request($"{_basePath}{_controllerName}", "Avatar");
        if (userId is not null)
            request = request.SetQueryParam("userId", userId.Value);

        return request.PostMultipartAsync(mp => mp.AddFile("file", image, fileName))
                      .ReceiveJson<AvatarUploadResponse>();
    }

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
