using Mars.Core.Models;
using Mars.Contracts.Common;
using Mars.Server.Contracts.Options;
using Mars.Notifications.Abstractions;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class OptionServiceClient : BasicServiceClient, IOptionServiceClient
{
    public OptionServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Option";
    }

    public Task<SiteSettings> GetSysOptions()
        => _client.Request($"{_basePath}{_controllerName}", "SysOptions")
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<SiteSettings>();

    public Task SaveSysOptions(SiteSettings value)
        => _client.Request($"{_basePath}{_controllerName}", "SysOptions")
                    .PutJsonAsync(value);

    public Task<T?> GetOption<T>()
        => _client.Request($"{_basePath}{_controllerName}", "Option", typeof(T).Name)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<T?>();

    public Task SaveOption<T>(T value)
        => _client.Request($"{_basePath}{_controllerName}", "Option", typeof(T).Name)
                    .PutJsonAsync(value);

    public Task SetLanguage(string culture, string returnUrl)
    {
        throw new NotImplementedException();
    }

    public Task<UserActionResult> SendTestEmail(TestMailMessage request)
        => _client.Request($"{_basePath}{_controllerName}", "SendTestEmail")
                    .PostJsonAsync(request)
                    .ReceiveJson<UserActionResult>();

    public Task<UserActionResult> SendTestSms(SendSmsModelRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "SendTestSms")
                    .PostJsonAsync(request)
                    .ReceiveJson<UserActionResult>();

}
