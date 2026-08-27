using Mars.Admin.Framework.Components;
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
            //Height = "240px",
            //Title = $"Updating the {DialogData.Name} sheet",
            PreventDismissOnOverlayClick = false,
            PreventScroll = true,
            Modal = true,
            TrapFocus = false,
            //Class = "DeletionConfirmationDialog" class not support
        });

        var result = await dialog.Result;

        return !result.Cancelled;
    }

}
