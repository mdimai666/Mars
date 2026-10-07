using Mars.Admin.Shared.Cloudy;
using Mars.Contracts.Common;
using Mars.Identity.Contracts.Roles;
using Mars.Identity.Contracts.Users;
using Mars.Identity.Contracts.UserTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.UserViews;

public partial class UsersPage
{
    string urlEditPage = "/dev/EditUser";

    [Inject] IMarsWebApiClient _client { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;

    FluentDataGrid<UserDetailResponse> _grid = default!;
    GridItemsProvider<UserDetailResponse> _dataProvider = default!;

    string _searchText = "";
    string? _roleFilter;
    int? _total;
    IReadOnlyDictionary<string, KpiResult> _kpi = new Dictionary<string, KpiResult>();
    IReadOnlyCollection<RoleSummaryResponse> _availRoles = [];

    IReadOnlyCollection<string> RoleNames => _availRoles.Select(r => r.Name).ToList();

    protected override async Task OnInitializedAsync()
    {
        _dataProvider = CloudyGridProvider.Create<UserDetailResponse>(LoadUsers);
        _availRoles = (await _client.Role.List(new())).Items;
        await LoadKpi();
    }

    async Task<ListDataResult<UserDetailResponse>> LoadUsers(int skip, int take, string? sort)
    {
        var data = await _client.User.ListDetail(new()
        {
            Skip = skip,
            Take = take,
            Sort = sort ?? nameof(UserDetailResponse.CreatedAt), // FullName [NotMapped] — сортировка только по маппед-полям
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
            Roles = _roleFilter is null ? null : [_roleFilter],
        });

        _total = data.TotalCount ?? data.Items.Count;
        StateHasChanged();
        return data;
    }

    async Task LoadKpi()
    {
        _kpi = await _client.Kpi.Get([UserKpiKeys.Total, UserKpiKeys.NewThisMonth]);
    }

    string KpiLabel(string key)
        => _kpi.TryGetValue(key, out var r) && !string.IsNullOrEmpty(r.Label) ? L[r.Label].Value : key;

    string KpiValue(string key)
        => _kpi.TryGetValue(key, out var r) ? r.Value.ToString("N0") : "—";

    void RefreshGrid() => _grid?.RefreshDataAsync();

    void OnSearchChanged(string text)
    {
        _searchText = text;
        RefreshGrid();
    }

    void SetRoleFilter(string? role)
    {
        _roleFilter = role;
        RefreshGrid();
    }

    public async Task Delete(Guid id)
    {
        await _client.User.Delete(id).SmartDelete();
        RefreshGrid();
        await LoadKpi();
    }

    public async Task HandleSearchInput()
    {
        RefreshGrid();
        await LoadKpi();
    }

    static int TintFor(Guid id) => (id.GetHashCode() & 0x7FFFFFFF) % 5;

    int TintIndex(string role)
    {
        var i = _availRoles.Select(r => r.Name).ToList()
            .FindIndex(n => string.Equals(n, role, StringComparison.OrdinalIgnoreCase));
        return (i < 0 ? 0 : i) % 5;
    }

    int? RoleTint(string role)
        => role.Equals("admin", StringComparison.OrdinalIgnoreCase) ? null : TintIndex(role);

    IReadOnlyCollection<RoleSummaryResponse>? rolesForCreate;
    IReadOnlyCollection<UserTypeListItemResponse>? userTypesForCreate;

    public async Task OnClickCreateUser()
    {
        rolesForCreate ??= (await _client.Role.List(new() { Take = 20 })).Items;
        userTypesForCreate ??= (await _client.UserType.List(new() { Take = 20 })).Items;

        var formData = new CreateUserEditFormData
        {
            Model = new(),
            Roles = rolesForCreate,
            DefaultCreateRole = null,
            UserTypes = userTypesForCreate,
        };
        formData.Model.Type = userTypesForCreate.FirstOrDefault(s => s.TypeName == "default")?.TypeName
                              ?? userTypesForCreate.FirstOrDefault()?.TypeName
                              ?? "";

        var result = await _dialogService.ShowDialogAsync<CreateUserDialog>(new DialogOptions
        {
            Modal = true,
            Header = { CloseAction = { Visible = true } },
            Parameters = { ["Content"] = formData }
        });
        if (!result.Cancelled)
        {
            await HandleSearchInput();
        }
    }

    private async Task OnClickChangePassword(UserDetailResponse user)
    {
        await _dialogService.ShowDialogAsync<ChangePasswordDialog>(new DialogOptions
        {
            Modal = true,
            Parameters =
            {
                ["Content"] = new ChangePasswordModel
                {
                    UserId = user.Id,
                    NewPassword = "",
                },
            },
        });
    }

    private async Task OnUserMenuClick(MenuItemEventArgs args, UserDetailResponse user)
    {
        switch (args.Item?.Id)
        {
            case "changepassword":
                await OnClickChangePassword(user);
                break;
            case "sendinvitation":
                SendInvation(user.Id);
                break;
        }
    }

    private void SendInvation(Guid userId)
    {
        _ = _client.User.SendInvation(userId).SmartActionResult();
    }
}
