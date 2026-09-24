using Mars.Admin.Framework;
using Mars.Identity.Contracts.Passkeys;
using Mars.Identity.Contracts.Options;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using IMessageService = Mars.Admin.Framework.Interfaces.IMessageService;

namespace Mars.Admin.Pages.UserViews;

public partial class PasskeysPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IMessageService messageService { get; set; } = default!;
    [Inject] PasskeyJs passkeyJs { get; set; } = default!;

    FluentDataGrid<PasskeySummaryResponse> table = default!;
    GridItemsProvider<PasskeySummaryResponse> dataProvider = default!;
    List<PasskeySummaryResponse> passkeys = [];

    bool _passkeyAvailable;
    bool _passkeysEnabled = true;

    bool addVisible;
    string newName = "";

    bool renameVisible;
    string renameValue = "";
    PasskeySummaryResponse? renameTarget;

    protected override void OnParametersSet()
    {
        _passkeysEnabled = Q.Site.GetOption<PasskeyOption>()?.Enabled != false;

        dataProvider = new GridItemsProvider<PasskeySummaryResponse>(
            async req =>
            {
                passkeys = (await client.Passkey.List()).ToList();
                return GridItemsProviderResult.From(passkeys, passkeys.Count);
            });
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _passkeyAvailable = await passkeyJs.IsAvailable();
            StateHasChanged();
        }
    }

    void OnClickAdd()
    {
        newName = "";
        addVisible = true;
    }

    async Task AddPasskey()
    {
        addVisible = false;

        try
        {
            var result = await passkeyJs.RegisterPasskey(string.IsNullOrWhiteSpace(newName) ? null : newName.Trim());

            if (result.Ok)
            {
                await table.RefreshDataAsync();
                _ = messageService.Success(result.Message);
            }
            else
            {
                _ = messageService.Error(result.Message);
            }
        }
        catch (Exception ex)
        {
            _ = messageService.Error(ex.Message);
        }
    }

    void OnClickRename(PasskeySummaryResponse passkey)
    {
        renameTarget = passkey;
        renameValue = passkey.Name ?? "";
        renameVisible = true;
    }

    async Task RenamePasskey()
    {
        if (renameTarget is null) return;

        if (string.IsNullOrWhiteSpace(renameValue))
        {
            _ = messageService.Error("Укажите название");
            return;
        }

        var result = await client.Passkey.Rename(new RenamePasskeyRequest
        {
            CredentialId = renameTarget.CredentialId,
            Name = renameValue.Trim(),
        });

        if (result.Ok)
        {
            renameVisible = false;
            await table.RefreshDataAsync();
            _ = messageService.Success(result.Message);
        }
        else
        {
            _ = messageService.Error(result.Message);
        }
    }

    async Task Delete(string credentialId)
    {
        var result = await client.Passkey.Delete(credentialId);

        if (result.Ok)
        {
            await table.RefreshDataAsync();
            _ = messageService.Success(result.Message);
        }
        else
        {
            _ = messageService.Error(result.Message);
        }
    }
}
