using Mars.Core.Models;
using Mars.Contracts.Common;
using Mars.Notifications.Abstractions;
using Mars.Contracts.Options;

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
