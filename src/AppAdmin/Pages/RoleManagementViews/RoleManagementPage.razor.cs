using AntDesign;
using AntDesign.TableModels;
using AppShared.Dto;
using AppShared.Features;
using AppShared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Remote.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppAdmin.Pages.RoleManagementViews
{
    public partial class RoleManagementPage
    {

        bool Busy = false;

        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public MessageService messageService { get; set; } = default!;
        [Inject] public RoleService service { get; set; } = default!;

        EditRolesViewModelDto model = default!;
        RoleClaimsDto? selRoleClaim = null;

        protected override void OnInitialized()
        {
            Load();
        }

        async void Load()
        {
            Busy = true;
            StateHasChanged();

            model = await service.EditRolesViewModel();

            Busy = false;
            StateHasChanged();
        }

        async void SaveClick()
        {
            //await Task.Delay(1000);
            //_ = messageService.Error("Не удалось сохранить");

            var res = await service.SaveRoleClaims(model);

            if (res.Ok)
            {
                _ = messageService.Success(res.Message);
            }
            else
            {
                _ = messageService.Error(res.Message);
            }

        }

        async void TestClaimHost()
        {
            //await Task.Delay(1000);
            //_ = messageService.Error("Не удалось сохранить");

            var res = await service.TestClaim();

            if (res.Ok)
            {
                _ = messageService.Success(res.Message);
            }
            else
            {
                _ = messageService.Error(res.Message);
            }

        }

        void TestClaimAppFront()
        {
            //RoleCaps.Post.Add;
            throw new NotImplementedException();
        }
    }
}
