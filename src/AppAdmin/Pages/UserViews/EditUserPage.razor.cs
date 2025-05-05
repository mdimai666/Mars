using AppFront.Shared.Interfaces;
using Mars.Core.Exceptions;
using Mars.Shared.Contracts.Roles;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Pages.UserViews;

public partial class EditUserPage
{
    [Parameter] public Guid ID { get; set; }

    [Inject] IMessageService messageService { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;


    StandartEditForm1<EditUserModel> _editForm1 = default!;
    bool isCreateNew => ID == Guid.Empty;

    IEnumerable<RoleSummaryResponse> availRoles = [];

    public async Task<EditUserModel> GetAction(Guid id)
    {
        availRoles = (await client.Role.List(new() { Take = 20 })).Items;

        if (isCreateNew) return new();

        var user = await client.User.Get(id) ?? throw new NotFoundException();
        return EditUserModel.ToModel(user, availRoles);
    }

    void AfterSave(EditUserModel model)
    {
        if (isCreateNew)
        {
            navigationManager.NavigateTo($"/dev/Users/{model.Id}");
            _ = messageService.Success(AppRes.SavedSuccessfully);

        }
    }

    void AfterDelete()
    {
        navigationManager.NavigateTo("/dev/Users");
        _ = messageService.Success(AppRes.DeletedSuccessfully);

    }

    async void UpdateUserRoles()
    {
        //await client.User.UpdateUserRoles(model.Id, UpdUserRoles).SmartActionResult();
    }

    async void OnClickSelectAvatar()
    {
        //var file = await mediaService.OpenSelectMedia();

        //if (file is not null)
        //{
        //    //Console.WriteLine("avatar file: " + file.FileUrlRelative);
        //    if (!Q.HostingInfo.UrlIsImage(file.UrlRelative))
        //    {
        //        _ = messageService.Error("Надо выбрать картинку");
        //        return;
        //    }
        //    model.AvatarUrl = file.FileUrlRelative;
        //    StateHasChanged();
        //}
    }

    void ClearAvatar()
    {
        //model.AvatarUrl = "";
    }

}
