using Mars.Contracts.Common;
using Mars.Notifications.Abstractions;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class NotificationsServiceClient : BasicServiceClient, INotificationsServiceClient
{
    public NotificationsServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Notifications";
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
