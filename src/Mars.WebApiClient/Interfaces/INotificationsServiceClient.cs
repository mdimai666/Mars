using Mars.Contracts.Common;
using Mars.Notifications.Contracts;

namespace Mars.WebApiClient.Interfaces;

public interface INotificationsServiceClient
{
    Task<UserActionResult> SendTestEmail(TestMailMessage request);
    Task<UserActionResult> SendTestSms(SendSmsModelRequest request);
}
