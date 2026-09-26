using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Bridges;

public class FluentMessageServiceBridge : Interfaces.IMessageService
{
    private readonly INotificationService _notifications;

    public FluentMessageServiceBridge(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public void Dispose()
    {
    }

    public Task Error(string content, double? durationMs = null, Action? onClose = null)
        => ShowAsync(ToastIntent.Error, content, durationMs, onClose);

    public Task Info(string content, double? durationMs = null, Action? onClose = null)
        => ShowAsync(ToastIntent.Info, content, durationMs, onClose);

    public Task Success(string content, double? durationMs = null, Action? onClose = null)
        => ShowAsync(ToastIntent.Success, content, durationMs, onClose);

    public Task Warning(string content, double? durationMs = null, Action? onClose = null)
        => ShowAsync(ToastIntent.Warning, content, durationMs, onClose);

    public Task Show(string content, Mars.Core.Models.MessageIntent messageIntent, double? durationMs = null, Action? onClose = null)
        => ShowAsync(
            messageIntent switch
            {
                Mars.Core.Models.MessageIntent.Error => ToastIntent.Error,
                Mars.Core.Models.MessageIntent.Info => ToastIntent.Info,
                Mars.Core.Models.MessageIntent.Success => ToastIntent.Success,
                Mars.Core.Models.MessageIntent.Warning => ToastIntent.Warning,
                Mars.Core.Models.MessageIntent.Custom => ToastIntent.Info,
                _ => throw new NotImplementedException()
            },
            content, durationMs, onClose);

    // await завершается при закрытии тоста (ResultTiming.Closed по умолчанию) — как onClose в v4
    private async Task ShowAsync(ToastIntent intent, string content, double? durationMs, Action? onClose)
    {
        await _notifications.ShowToastAsync(options =>
        {
            options.Intent = intent;
            options.Title = content;
            if (durationMs is { } ms)
                options.Lifetime = TimeSpan.FromMilliseconds(ms);
        });

        onClose?.Invoke();
    }
}
