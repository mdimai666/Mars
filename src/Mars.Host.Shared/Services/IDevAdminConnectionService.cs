using Mars.Core.Models;

namespace Mars.Host.Shared.Services;

public interface IDevAdminConnectionService
{
    Task ShowNotifyMessage(string message, MessageIntent? messageIntent = MessageIntent.Info);
}