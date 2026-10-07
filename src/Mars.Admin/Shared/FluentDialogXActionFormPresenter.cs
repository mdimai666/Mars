using Mars.XActions.Contracts;
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

        var result = await dialogService.ShowDialogAsync(componentType, new DialogOptions
        {
            Header = { Title = command.Label },
            Width = "480px",
            Parameters = { ["Content"] = command },
        });

        if (result is { Cancelled: false } && result.Value is IReadOnlyDictionary<string, string> values)
            return values;

        return null;
    }
}
