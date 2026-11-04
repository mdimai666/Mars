using Mars.Admin.Framework.Components.Forms;
using Mars.Contracts.Resources;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using IMessageService = Mars.Admin.Framework.Interfaces.IMessageService;

namespace Mars.Admin.SiteSpace.Pages.UserViews;

public partial class EditUserPage
{
    [Parameter] public Guid ID { get; set; }

    [SupplyParameterFromQuery] public string UserTypeName { get; set; } = "default";

    [Inject] IMessageService messageService { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    StandardEditForm1<UserEditModel> _editForm1 = default!;
    MetaValuesForm? _metaValueForm;

    UserEditModel? _model;
    bool _editing;

    bool isCreateNew => ID == Guid.Empty;

    protected override async Task OnParametersSetAsync()
    {
        if (_model is null || _model.Id != ID)
            await LoadModel();
    }

    async Task LoadModel()
    {
        _model = await UserEditModel.GetAction(client, ID, UserTypeName);
    }

    void StartEdit() => _editing = true;

    async Task CancelEdit()
    {
        _editing = false;
        await LoadModel();
    }

    static string Value(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s;

    Task OnBeforeSave(UserEditModel model)
        => _metaValueForm is null ? Task.CompletedTask : _metaValueForm.CommitAllAsync();

    async Task AfterSave(UserEditModel model)
    {
        if (isCreateNew)
        {
            navigationManager.NavigateTo($"/dev/Users/{model.Id}");
            _ = messageService.Success(AppRes.SavedSuccessfully);
            return;
        }

        _editing = false;
        await LoadModel();
    }

    void AfterDelete()
    {
        navigationManager.NavigateTo("/dev/Users");
        _ = messageService.Success(AppRes.DeletedSuccessfully);
    }

    async Task OnMenuClick(MenuItemEventArgs args)
    {
        switch (args.Item?.Id)
        {
            case "changepassword":
                await dialogService.ShowDialogAsync<ChangePasswordDialog>(new DialogOptions
                {
                    Modal = true,
                    Parameters =
                    {
                        ["Content"] = new ChangePasswordModel
                        {
                            UserId = ID,
                            NewPassword = "",
                        },
                    },
                });
                break;
            case "sendinvitation":
                _ = client.User.SendInvation(ID).SmartActionResult();
                break;
        }
    }
}
