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

        var result = await dialogService.ShowDialogAsync<DeleteConfirmationDialog>(new DialogOptions
        {
            Modal = true,
            Parameters = { ["Content"] = content },
        });

        return !result.Cancelled;
    }

}
