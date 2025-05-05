using AppFront.Shared.Components;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppFront.Shared.Extensions;

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
            //Class = "DeletionConfirmationDialog" class not support
        });

        var result = await dialog.Result;

        return !result.Cancelled;
    }

}
