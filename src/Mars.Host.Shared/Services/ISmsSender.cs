using Mars.Shared.Common;
using Mars.Shared.Contracts.Sms;

namespace Mars.Host.Shared.Services;

public interface ISmsSender
{
    Task<UserActionResult> Send(SendSmsModelRequest form);
    Task<UserActionResult> SendTestSms(SendSmsModelRequest form);
}