using Mars.Admin.Framework.Services;
using Mars.Contracts.XActions;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Shared;

/// <summary>
/// Показ формы аргументов XAction диалогом FluentUI: кастомная форма из
/// <see cref="IXActionFormProvider"/> или генерик <see cref="XActionFormDialog"/>.
/// </summary>
internal class FluentDialogXActionFormPresenter(IDialogService dialogService, IXActionFormProvider formProvider) : IXActionFormPresenter
{
    public async Task<IReadOnlyDictionary<string, string>?> ShowFormAsync(XActionCommand command)
    {
        var componentType = formProvider.GetForm(command.Id) ?? typeof(XActionFormDialog);

        var parameters = new DialogParameters
        {
            Title = command.Label,
            Width = "480px",
            PreventScroll = true,
        };

        var dialog = await dialogService.ShowDialogAsync(componentType, command, parameters);
        var result = await dialog.Result;

        if (result is { Cancelled: false } && result.Data is IReadOnlyDictionary<string, string> values)
            return values;

        return null;
    }
}
