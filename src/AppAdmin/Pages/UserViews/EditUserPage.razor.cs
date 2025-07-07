using AppFront.Shared.Interfaces;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Pages.UserViews;

public partial class EditUserPage
{
    [Parameter] public Guid ID { get; set; }

    [SupplyParameterFromQuery] public string UserTypeName { get; set; } = "default";

    [Inject] IMessageService messageService { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;

    StandartEditForm1<UserEditModel> _editForm1 = default!;
    bool isCreateNew => ID == Guid.Empty;

    void AfterSave(UserEditModel model)
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

    void UpdateUserRoles()
    {
        //await client.User.UpdateUserRoles(model.Id, UpdUserRoles).SmartActionResult();
    }

    void OnClickSelectAvatar()
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
