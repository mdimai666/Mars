using Mars.Contracts.Common;
using Mars.Notifications.Contracts;

namespace Mars.Notifications.Abstractions;

public interface ISmsSender
{
    Task<UserActionResult> Send(SendSmsModelRequest form);
    Task<UserActionResult> SendTestSms(SendSmsModelRequest form);
}