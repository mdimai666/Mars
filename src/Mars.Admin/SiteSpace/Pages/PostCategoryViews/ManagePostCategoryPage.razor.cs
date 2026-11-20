using Mars.Admin.Shared.Cloudy;
using Mars.Cms.Contracts.PostCategories;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Common;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.SiteSpace.Pages.PostCategoryViews;

public partial class ManagePostCategoryPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    [Parameter]
    public string POSTTYPE { get; set; } = "post";

    PostTypeAdminPanelItemResponse postType = Q.Site.PostTypes.First(s => s.TypeName == "post");

    public static string GetPageLink(string postType) => $"PostCategory/{postType}";

    string prevPostType = "";
    string urlEditPage = "/dev/EditPostCategory";

    //table
    FluentDataGrid<PostCategoryListItemResponse> _grid = default!;
    GridItemsProvider<PostCategoryListItemResponse> _dataProvider = default!;
    string _searchText = "";
    int? _total;

    IReadOnlyDictionary<string, KpiResult> _kpi = new Dictionary<string, KpiResult>();

    protected override void OnInitialized()
    {
        // loader читает актуальный postType в момент вызова — при смене типа
        // провайдер не пересоздаётся, достаточно RefreshDataAsync
        _dataProvider = CloudyGridProvider.Create<PostCategoryListItemResponse>(LoadCategories);
    }

    protected override async Task OnParametersSetAsync()
    {
        // на маршруте "/PostCategory" без сегмента POSTTYPE приходит пустой строкой — нормализуем к "post"
        var typeName = string.IsNullOrEmpty(POSTTYPE) ? "post" : POSTTYPE;
        if (prevPostType != typeName)
        {
            prevPostType = typeName;
            postType = Q.Site.PostTypes.FirstOrDefault(s => s.TypeName == typeName) ?? Q.Site.PostTypes.First(s => s.TypeName == "post");

            await LoadKpi();
            Refresh();
        }
    }

    async Task<ListDataResult<PostCategoryListItemResponse>> LoadCategories(int skip, int take, string? sort)
    {
        var data = await client.PostCategory.ListForPostType(postType.TypeName, new()
        {
            Skip = skip,
            Take = take,
            // дефолт — SlugPath ascending: сохраняет порядок дерева категорий
            Sort = sort ?? nameof(PostCategoryListItemResponse.SlugPath),
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
        });

        _total = data.TotalCount ?? data.Items.Count;
        StateHasChanged();
        return data;
    }

    async Task LoadKpi()
    {
        try
        {
            _kpi = await client.Kpi.Get([PostCategoryKpiKeys.TotalFor(postType.TypeName)]);
        }
        catch
        {
            // KPI справочные: список работает независимо от них
        }
        StateHasChanged();
    }

    string KpiLabel(string key)
        => _kpi.TryGetValue(key, out var r) && !string.IsNullOrEmpty(r.Label) ? L[r.Label].Value : key;

    string KpiValue(string key)
        => _kpi.TryGetValue(key, out var r) ? r.Value.ToString("N0") : "—";

    /// <summary>Отступ иерархии в пикселях: (LevelsCount-1)*20</summary>
    static int IndentPx(PostCategoryListItemResponse item) => (item.LevelsCount - 1) * 20;

    void OnSearchChanged(string text)
    {
        _searchText = text;
        Refresh();
    }

    async Task Delete(Guid id)
    {
        await client.PostCategory.Delete(id).SmartDelete();
        Refresh();
        await LoadKpi();
    }

    void Refresh() => _grid?.RefreshDataAsync();
}
