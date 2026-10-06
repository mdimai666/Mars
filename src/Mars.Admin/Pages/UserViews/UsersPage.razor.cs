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

    FluentDataGrid<UserDetailResponse> _grid = default!;
    GridItemsProvider<UserDetailResponse> _dataProvider = default!;

    string _searchText = "";
    string? _roleFilter;
    int? _total;
    int? _totalAll;
    int? _newThisMonth;
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
        if (_roleFilter is null && string.IsNullOrWhiteSpace(_searchText))
        {
            _totalAll = _total;
        }
        StateHasChanged();
        return data;
    }

    async Task LoadKpi()
    {
        var all = await _client.User.ListDetail(new() { Take = 1, Sort = "LastName" });
        _totalAll = all.TotalCount ?? 0;

        var now = DateTimeOffset.Now;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        var fresh = await _client.User.ListDetail(new() { Take = 1, Sort = "LastName", CreatedFrom = monthStart });
        _newThisMonth = fresh.TotalCount ?? 0;
    }

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

    static string Fmt(int? v) => v?.ToString() ?? "—";

    static int TintFor(Guid id) => (id.GetHashCode() & 0x7FFFFFFF) % 5;

    int TintIndex(string role)
    {
        var i = _availRoles.Select(r => r.Name).ToList()
            .FindIndex(n => string.Equals(n, role, StringComparison.OrdinalIgnoreCase));
        return (i < 0 ? 0 : i) % 5;
    }

    int? RoleTint(string role)
        => role.Equals("admin", StringComparison.OrdinalIgnoreCase) ? null : TintIndex(role);

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

    private void OnUserMenuClick(MenuItemEventArgs args, UserDetailResponse user)
    {
        switch (args.Item?.Id)
        {
            case "changepassword":
                OnClickChangePassword(user);
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
