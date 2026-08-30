using Mars.Contracts.Common;

namespace Mars.Notifications.Abstractions;

public interface INotifyService
{
    Task<UserActionResult> SendNotifyTest(Guid userId, CancellationToken cancellationToken);
    Task<UserActionResult> SendNotify(string title, string body, Guid userId, CancellationToken cancellationToken);
    Task<UserActionResult> SendNotify_Invation(Guid userId, CancellationToken cancellationToken);

}
