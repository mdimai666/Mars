using System.Collections.ObjectModel;
using AppAdmin.Pages.FeedbackViews;
using AppFront.Main.Extensions;
using Mars.Shared.Contracts.Roles;
using Mars.Shared.Contracts.Users;
using Mars.Shared.Contracts.UserTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using IMessageService = AppFront.Shared.Interfaces.IMessageService;

namespace AppAdmin.Pages.UserViews;

public partial class UsersPage
{
    string urlEditPage = "/dev/EditUser";

    [Inject] IMarsWebApiClient _client { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] IMessageService _messageService { get; set; } = default!;

    FluentDataGrid<UserDetailResponse> table = default!;
    string _searchText = "";
    IEnumerable<string> _roleFilter = [];
    ListDataResult<UserDetailResponse> data = ListDataResult<UserDetailResponse>.Empty();

    GridItemsProvider<UserDetailResponse> dataProvider = default!;

    IReadOnlyCollection<RoleSummaryResponse> _availRoles = [];

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<UserDetailResponse>(
            async req =>
            {
                var sortColumn = req.GetSortByProperties().Count == 0
                                        ? nameof(UserDetailResponse.FullName)
                                        : req.GetSortByProperties().First().PropertyName;
                var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                //_roleFilter

                data = await _client.User.ListDetail(new()
                {
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,

                    Roles = _roleFilter.ToList(),
                });

                var collection = new Collection<UserDetailResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );

        //_ = Load();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Task.Run(async () =>
            {
                _availRoles = (await _client.Role.List(new())).Items;
            });
        }
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    async void OnRowClick(FluentDataGridRow<UserDetailResponse> row)
    {

        if (row.Item is null) return;
        return;

        DialogParameters parameters = new()
        {
            Title = row.Item.FullName,
            //PrimaryActionEnabled = false,
            //PrimaryAction = "Yes",
            SecondaryAction = null,
            //Width = "500px",
            //TrapFocus = _trapFocus,
            //Modal = _modal,
            PreventScroll = true
        };

        var detail = await _client.NavMenu.Get(row.Item.Id);

        if (detail is not null)
        {
            IDialogReference dialog = await _dialogService.ShowDialogAsync<ViewFeedbackDialog>(detail, parameters);
            DialogResult? result = await dialog.Result;
        }
        else
        {
            _ = _messageService.Error("element not found");
        }

    }

    public async Task Delete(Guid id)
    {
        await _client.User.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }

    bool visibleCreateUserModal;
    CreateUserEditFormData createFormData = new();
    IReadOnlyCollection<RoleSummaryResponse>? rolesForCreate;
    IReadOnlyCollection<UserTypeListItemResponse>? userTypesForCreate;

    public async Task OnClickCreateUser()
    {
        rolesForCreate ??= (await _client.Role.List(new() { Take = 20 })).Items;
        userTypesForCreate ??= (await _client.UserType.List(new() { Take = 20 })).Items;

        createFormData = new()
        {
            Model = new(),
            Roles = rolesForCreate,
            DefaultCreateRole = null,
            UserTypes = userTypesForCreate,
        };
        createFormData.Model.Type = userTypesForCreate.FirstOrDefault(s => s.TypeName == "default")?.TypeName
                                    ?? userTypesForCreate.FirstOrDefault()?.TypeName
                                    ?? "";

        visibleCreateUserModal = true;

    }

    bool visibleChangeUserPasswordModal;
    ChangePasswordModel changeUserPasswordFormData = new();

    private void OnClickChangePassword(UserDetailResponse user)
    {
        changeUserPasswordFormData = new()
        {
            UserId = user.Id,
            NewPassword = "",
        };
        visibleChangeUserPasswordModal = true;
    }

    private void SendInvation(Guid userId)
    {
        _ = _client.User.SendInvation(userId).SmartActionResult();
    }
}
