namespace AppFront.Shared.Interfaces;

public interface IMessageService : IDisposable
{
    public Task Success(string content, double? duration = null, Action? onClose = null);

    public Task Error(string content, double? duration = null, Action? onClose = null);

    public Task Info(string content, double? duration = null, Action? onClose = null);

    public Task Warning(string content, double? duration = null, Action? onClose = null);

    public Task Show(string content, Mars.Core.Models.MessageIntent messageIntent, double? duration = null, Action? onClose = null);
}
