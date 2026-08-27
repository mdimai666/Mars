using Mars.Shared.Common;
using Mars.Shared.Contracts.Sms;

namespace Mars.Notifications.Abstractions;

public interface ISmsSender
{
    Task<UserActionResult> Send(SendSmsModelRequest form);
    Task<UserActionResult> SendTestSms(SendSmsModelRequest form);
}