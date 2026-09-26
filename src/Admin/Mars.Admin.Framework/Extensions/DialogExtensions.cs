using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Dialogs;
using Mars.Contracts.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Extensions;

public static class DialogExtensions
{
    public static async Task<bool> MarsDeleteConfirmation(this IDialogService dialogService, string? message = null)
    {
        var content = (MarkupString)(message ?? AppRes.DeletionConfirmationMessage);

        var dialog = await dialogService.ShowDialogAsync<DeleteConfirmationDialog>(content, new DialogParameters()
        {
            Modal = true,
        });

        var result = await dialog.Result;

        return !result.Cancelled;
    }

}
