using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppFront.Shared.Bridges;

public class FluentMessageServiceBridge : Interfaces.IMessageService
{
    private readonly IToastService _toastService;

    public FluentMessageServiceBridge(IToastService toastService)
    {
        _toastService = toastService;
    }
    public void Dispose()
    {
    }

    public Task Error(string content, double? duration = null, Action? onClose = null)
    {
        EventCallback<ToastResult>? callback = onClose is null ? null : new EventCallbackFactory().Create<ToastResult>(this, onClose);
        _toastService.ShowError(content, (int?)duration, callback: callback);
        return Task.CompletedTask;
    }

    public Task Info(string content, double? duration = null, Action? onClose = null)
    {
        EventCallback<ToastResult>? callback = onClose is null ? null : new EventCallbackFactory().Create<ToastResult>(this, onClose);
        _toastService.ShowInfo(content, (int?)duration, callback: callback);
        return Task.CompletedTask;
    }

    public Task Success(string content, double? duration = null, Action? onClose = null)
    {
        EventCallback<ToastResult>? callback = onClose is null ? null : new EventCallbackFactory().Create<ToastResult>(this, onClose);
        _toastService.ShowSuccess(content, (int?)duration, callback: callback);
        return Task.CompletedTask;
    }

    public Task Warning(string content, double? duration = null, Action? onClose = null)
    {
        EventCallback<ToastResult>? callback = onClose is null ? null : new EventCallbackFactory().Create<ToastResult>(this, onClose);
        _toastService.ShowWarning(content, (int?)duration, callback: callback);
        return Task.CompletedTask;
    }

    public Task Show(string content, Mars.Core.Models.MessageIntent messageIntent, double? duration = null, Action? onClose = null)
    {
        EventCallback<ToastResult>? callback = onClose is null ? null : new EventCallbackFactory().Create<ToastResult>(this, onClose);

        _toastService.ShowToast(
            messageIntent switch
            {
                Mars.Core.Models.MessageIntent.Error => ToastIntent.Error,
                Mars.Core.Models.MessageIntent.Info => ToastIntent.Info,
                Mars.Core.Models.MessageIntent.Success => ToastIntent.Success,
                Mars.Core.Models.MessageIntent.Warning => ToastIntent.Warning,
                Mars.Core.Models.MessageIntent.Custom => ToastIntent.Info,
                _ => throw new NotImplementedException()
            },
            content,
            (int?)duration,
            callback: callback
        );
        return Task.CompletedTask;

    }
}
