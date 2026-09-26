using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Dialogs;

/// <summary>
/// v4-совместимые вызовы диалогов поверх v5 <see cref="IDialogService"/>:
/// content передаётся параметром "Content" в компонент диалога, результат — v5 <see cref="DialogResult"/>.
/// </summary>
public static class DialogServiceCompatExtensions
{
    public const string ContentParameterName = "Content";

    public static Task<IDialogReference> ShowDialogAsync<TDialog>(
        this IDialogService dialogService, object? content = null, DialogParameters? parameters = null)
        where TDialog : ComponentBase
    {
        var task = dialogService.ShowDialogAsync<TDialog>(MapOptions(content, parameters));
        return Task.FromResult<IDialogReference>(new DialogReference(task));
    }

    public static Task<IDialogReference> ShowDialogAsync(
        this IDialogService dialogService, Type dialogType, object? content = null, DialogParameters? parameters = null)
    {
        var task = dialogService.ShowDialogAsync(dialogType, MapOptions(content, parameters));
        return Task.FromResult<IDialogReference>(new DialogReference(task));
    }

    public static Task<IDialogReference> ShowPanelAsync<TDialog>(
        this IDialogService dialogService, object? content = null, DialogParameters? parameters = null,
        DialogAlignment alignment = DialogAlignment.End)
        where TDialog : ComponentBase
    {
        var options = MapOptions(content, parameters);
        options.Alignment = alignment;
        var task = dialogService.ShowDrawerAsync<TDialog>(options);
        return Task.FromResult<IDialogReference>(new DialogReference(task));
    }

    private static DialogOptions MapOptions(object? content, DialogParameters? parameters)
    {
        var options = new DialogOptions();
        if (parameters is not null)
        {
            options.Header.Title = parameters.Title;
            options.Width = parameters.Width;
            options.Height = parameters.Height;
            options.Modal = parameters.Modal;
            if (parameters.ShowDismiss is { } showDismiss)
                options.Header.CloseAction.Visible = showDismiss;
        }

        if (content is not null)
            options.Parameters[ContentParameterName] = content;

        return options;
    }
}
