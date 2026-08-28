using Mars.Core.Models;
using Mars.Contracts.Common;
using Mars.Server.Contracts.Options;
using Mars.Notifications.Abstractions;

namespace Mars.WebApiClient.Interfaces;

public interface IOptionServiceClient
{
    Task<SysOptions> GetSysOptions();
    Task SaveSysOptions(SysOptions value);
    Task<T?> GetOption<T>();
    Task SaveOption<T>(T value);
    Task SetLanguage(string culture, string returnUrl);
    Task<UserActionResult> SendTestEmail(TestMailMessage request);
    Task<UserActionResult> SendTestSms(SendSmsModelRequest request);

}
