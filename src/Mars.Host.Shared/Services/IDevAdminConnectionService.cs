using Mars.Core.Models;

namespace Mars.Host.Shared.Services;

public interface IDevAdminConnectionService
{
    IReadOnlyCollection<PageContextInfo> GetPageContexts();
    Task ShowNotifyMessage(string message, string userId, MessageIntent? messageIntent = MessageIntent.Info);
    Task ShowNotifyMessageForAll(string message, MessageIntent? messageIntent = MessageIntent.Info);
}

public record PageContextInfo(string PageTypeName, string DisplayName);
