using Mars.Admin.Shared.Cloudy;
using Mars.Contracts.Common;
using Mars.Identity.Contracts.Roles;
using Mars.Identity.Contracts.ViewModels;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.SiteSpace.Pages.RoleViews;

public partial class RolesPage
{
    string urlEditPage = "/dev/EditRole";

    [Inject] IMarsWebApiClient _client { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] NavigationManager _navigation { get; set; } = default!;

    FluentDataGrid<RoleSummaryResponse> _grid = default!;
    GridItemsProvider<RoleSummaryResponse> _dataProvider = default!;

    string _searchText = "";
    int? _total;

    // матрица прав — источник Title (локализация сервера) и счётчиков прав
    EditRolesViewModelDto? _matrix;

    protected override async Task OnInitializedAsync()
    {
        _dataProvider = CloudyGridProvider.Create<RoleSummaryResponse>(LoadRoles);
        _matrix = await _client.Role.EditRolesViewModel();
    }

    async Task<ListDataResult<RoleSummaryResponse>> LoadRoles(int skip, int take, string? sort)
    {
        var data = await _client.Role.List(new ListRoleQueryRequest
        {
            Skip = skip,
            Take = take,
            Sort = sort ?? nameof(RoleSummaryResponse.CreatedAt),
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
        });

        _total = data.TotalCount ?? data.Items.Count;
        StateHasChanged();
        return data;
    }

    RoleClaimsDto? FindRole(Guid id)
        => _matrix?.RoleClaims.FirstOrDefault(r => r.Role.Id == id);

    string TitleFor(RoleSummaryResponse role)
        => FindRole(role.Id)?.Role.Title is { Length: > 0 } title ? title : role.Name;

    string CapCountText(Guid id)
    {
        var role = FindRole(id);
        if (role is null) return "—";
        var checkedCount = role.Groups.SelectMany(g => g.RoleCapElements).Count(c => c.Checked);
        var totalCount = role.Groups.SelectMany(g => g.RoleCapElements).Count();
        return $"{checkedCount} / {totalCount}";
    }

    static bool IsAdmin(string roleName)
        => roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    void RefreshGrid() => _grid?.RefreshDataAsync();

    void OnSearchChanged(string text)
    {
        _searchText = text;
        RefreshGrid();
    }

    public async Task Delete(Guid id)
    {
        await _client.Role.Delete(id).SmartDelete();
        _matrix = await _client.Role.EditRolesViewModel();
        RefreshGrid();
    }

    public async Task OnClickCreateRole()
    {
        var result = await _dialogService.ShowDialogAsync<CreateRoleDialog>(new DialogOptions
        {
            Modal = true,
            Header = { CloseAction = { Visible = true } },
            Parameters = { ["Content"] = new CreateRoleModel() },
        });

        if (!result.Cancelled && result.Value is RoleDetailResponse created)
        {
            _navigation.NavigateTo($"{urlEditPage}/{created.Id}");
        }
    }
}
