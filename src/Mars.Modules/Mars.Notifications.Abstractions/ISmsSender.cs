using Mars.Contracts.Common;

namespace Mars.Notifications.Abstractions;

public interface ISmsSender
{
    Task<UserActionResult> Send(SendSmsModelRequest form);
    Task<UserActionResult> SendTestSms(SendSmsModelRequest form);
}