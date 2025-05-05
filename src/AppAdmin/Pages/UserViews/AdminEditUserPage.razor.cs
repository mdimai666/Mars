using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using AntDesign;
using AppFront.Shared.AuthProviders;
using AppShared.Dto;
using AppShared.ViewModels;
using System.Collections.Generic;
using System.Linq;
using AntDesign.Internal;
using AppFront.Shared;
using AppShared.Models;
using Mars.Shared.Contracts.Users;

namespace AppAdmin.Pages.UserViews;

public partial class AdminEditUserPage
{
    Guid _id;
    [Parameter] public Guid ID { get => _id; set { _id = value; _ = basicPage?.StartLoad(); } }
    Guid userID => (Q.User.IsAdmin && ID != Guid.Empty) ? ID : Q.User.Id;

    public UserEditProfileResponse? user { get; set; }

    BasicPage<UserEditProfileResponse>? basicPage;

    [Inject] public MessageService messageService { get; set; } = default!;
    [Inject] public UserService userService { get; set; } = default!;
    [Inject] public NavigationManager navigationManager { get; set; } = default!;
    [Inject] public ViewModelService viewModelService { get; set; } = default!;
    [Inject] public MediaService mediaService { get; set; } = default!;


    IForm form = default!;

    EditUserViewModel? vm;

    public IEnumerable<Guid> UpdUserRoles { get => vm.User.Roles.Select(s => s.Id); set => vm.User.Roles = vm.Roles.Where(s => value.Contains(s.Id)); }


    async Task<UserEditProfileResponse> Get()
    {
        //return await userService.UserEditProfile(ID == Guid.Empty ? Q.User.Id : ID);
        vm = await viewModelService.EditUserViewModel(userID);

        return new UserEditProfileResponse(vm.User);
    }

    async void OnFinish(EditContext context)
    {

        if (user == null) throw new ArgumentNullException(nameof(user));

        var result = await userService.UserEditProfileUpdate(user);

        if (result.Ok)
        {
            _ = messageService.Success(result.Message);
            user = result.Data;

        }
        else
        {
            _ = messageService.Error(result.Message);
        }
        StateHasChanged();

    }

    async void OnDeleteClick()
    {
        var result = await userService.Delete(user.Id);

        if (result.Ok)
        {
            _ = messageService.Success(result.Message);
            navigationManager.NavigateTo("/dev/Users");
        }
        else
        {
            _ = messageService.Error(result.Message);
        }
    }

    async void DisableUser(bool setState)
    {
        vm.User.LockoutEnabled = setState;
        //Console.WriteLine($"LockoutEnabled={vm.User.LockoutEnabled}");

        var result = await userService.SetUserState(user.Id, setState);

        if (result.Ok)
        {
            _ = messageService.Success(result.Message);
        }
        else
        {
            _ = messageService.Error(result.Message);
        }

        StateHasChanged();
    }

    async void UpdateUserRoles()
    {
        var result = await userService.UpdateUserRoles(user.Id, UpdUserRoles);

        if (result.Ok)
        {
            _ = messageService.Success(result.Message);
        }
        else
        {
            _ = messageService.Error(result.Message);
        }
    }

    bool isCompany()
    {
        return user?.MetaValues?.FirstOrDefault(s => s.MetaField.Key == "iscompany")?.Bool == true;
    }

    async void OnClickSelectAvatar()
    {
        FileEntity file = await mediaService.OpenSelectMedia();

        if (file is not null)
        {
            //Console.WriteLine("avatar file: " + file.FileUrlRelative);
            if (!FileEntity.UrlIsImage(file.FileUrlRelative))
            {
                _ = messageService.Error("Надо выбрать картинку");
                return;
            }
            user.AvatarUrl = file.FileUrlRelative;
            StateHasChanged();
        }
    }

    void ClearAvatar()
    {
        user.AvatarUrl = "";
    }

}
