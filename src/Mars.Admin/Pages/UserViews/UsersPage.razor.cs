using Mars.Identity.Contracts.Roles;
using Mars.Identity.Contracts.Users;
using Mars.Identity.Contracts.UserTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.UserViews;

public partial class UsersPage
{
    const int PageSize = 50;

    string urlEditPage = "/dev/EditUser";

    [Inject] IMarsWebApiClient _client { get; set; } = default!;

    string _searchText = "";
    string? _roleFilter;
    int _skip;
    int? _total;
    int? _totalAll;
    int? _newThisMonth;
    bool _loaded;
    List<UserDetailResponse> _users = [];
    IReadOnlyCollection<RoleSummaryResponse> _availRoles = [];

    IReadOnlyCollection<string> RoleNames => _availRoles.Select(r => r.Name).ToList();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _availRoles = (await _client.Role.List(new())).Items;
            await Task.WhenAll(LoadUsers(), LoadKpi());
        }
        finally
        {
            _loaded = true;
        }
    }

    async Task LoadUsers()
    {
        var data = await _client.User.ListDetail(new()
        {
            Skip = _skip,
            Take = PageSize,
            Sort = "LastName",
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
            Roles = _roleFilter is null ? null : [_roleFilter],
        });

        _users = [.. data.Items];
        _total = data.TotalCount ?? _users.Count;
        if (_roleFilter is null && string.IsNullOrWhiteSpace(_searchText))
        {
            _totalAll = _total;
        }
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

    async Task OnSearchChanged(string text)
    {
        _searchText = text;
        _skip = 0;
        await LoadUsers();
    }

    async Task SetRoleFilter(string? role)
    {
        _roleFilter = role;
        _skip = 0;
        await LoadUsers();
    }

    async Task OnSkipChanged(int skip)
    {
        _skip = skip;
        await LoadUsers();
    }

    public async Task Delete(Guid id)
    {
        await _client.User.Delete(id).SmartDelete();
        await Task.WhenAll(LoadUsers(), LoadKpi());
    }

    public async Task HandleSearchInput()
    {
        _skip = 0;
        await Task.WhenAll(LoadUsers(), LoadKpi());
    }

    static string Fmt(int? v) => v?.ToString() ?? "—";

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
