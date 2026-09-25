using System.Collections.ObjectModel;
using Mars.Admin.Framework.Hub;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.PostsViews;

public partial class ManagePostView : IDisposable
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] ClientHub clientHub { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;

    [Parameter, EditorRequired]
    public PostTypeAdminPanelItemResponse PostType { get; set; } = default!;
    string previousRequestPostTypeName = "";

    string urlEditPage = "/dev/EditPost";
    string GridTemplateColumns = "3fr 2fr 1fr 1fr";

    //table
    FluentDataGrid<PostListItemResponse> table = default!;
    string _searchText = "";
    ListDataResult<PostListItemResponse> data = ListDataResult<PostListItemResponse>.Empty();
    GridItemsProvider<PostListItemResponse> dataProvider = default!;

    Guid _filterCategoryId;
    string prevPostTypeName = "";

    // динамические колонки из настроек презентации типа
    PostTypeGridSettings? _gridSettings;
    IReadOnlyCollection<MetaFieldDetailResponse> _metaFields = [];
    IReadOnlyCollection<PostStatusResponse> _postStatuses = [];
    List<GridColumn> _columns = [];
    int _gridVersion;

    // фильтры колонок (сессионно, без сохранения) — состояние в панели
    bool _filtersVisible;
    ManagePostFiltersPanel? _filtersPanel;
    List<GridColumn> _filterColumns = [];

    // диалог настройки колонок
    bool _settingsDialogVisible;
    PostTypeGridSettings? _gridDraft;

    protected override void OnInitialized()
    {
        clientHub.OnPostListChanged += OnPostListChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (prevPostTypeName != PostType.TypeName)
        {
            prevPostTypeName = PostType.TypeName;

            // презентация берётся с сервера — начальные данные сайта могли устареть
            var presentation = await client.PostType.GetPresentationEditModel(PostType.Id);
            _gridSettings = presentation.Presentation.Grid;

            var detail = await client.PostType.Get(PostType.Id);
            _metaFields = detail?.MetaFields ?? [];
            _postStatuses = detail?.PostStatusList ?? [];

            RebuildColumns();
            BuildDataProvider();
            Refresh();
        }

        if (previousRequestPostTypeName != PostType.TypeName)
        {
            previousRequestPostTypeName = PostType.TypeName;
        }
    }

    void BuildDataProvider()
    {
        dataProvider = new GridItemsProvider<PostListItemResponse>(
            async req =>
            {
                string sortColumn;
                bool ascending;
                if (req.GetSortByProperties().Count != 0)
                {
                    sortColumn = req.GetSortByProperties().First().PropertyName;
                    ascending = req.SortByAscending;
                }
                else
                {
                    // сортировка по умолчанию из настройки типа; запасная — дата создания
                    var def = _columns.FirstOrDefault(c => c.IsDefaultSort);
                    sortColumn = SortProperty(def);
                    ascending = def?.DefaultSortDirection != SortDirection.Descending;
                }

                var sort = (ascending ? "" : "-") + sortColumn;

                data = await client.Post.List(PostType.TypeName, new()
                {
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                    IncludeCategory = true,
                    CategoryId = _filterCategoryId == Guid.Empty ? null : _filterCategoryId,
                    FilterIncludeDescendantsCategories = false,
                    MetaFields = _columns.Where(c => !c.IsSystem).Select(c => c.Key).ToArray(),
                    Filters = _filtersPanel?.BuildGridFilters() ?? [],
                });

                var collection = new Collection<PostListItemResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    void RebuildColumns()
    {
        var available = PostTypeGridColumns.Available(PostType.EnabledFeatures, _metaFields);
        // скрытые настройкой колонки в грид не попадают
        var columns = PostTypeGridColumns.Merge(_gridSettings?.Columns, available)
                                         .Where(c => c.Visible)
                                         .Select(c => new GridColumn(c))
                                         .ToList();

        // сортировка по умолчанию — только базовые колонки; запасная — дата создания
        var sortKey = _gridSettings?.SortKey;
        var sortColumn = columns.FirstOrDefault(c => c.IsSystem && c.Key == sortKey)
                         ?? columns.FirstOrDefault(c => c.IsSystem && c.Key == SystemFieldsCatalog.CreatedAt);
        if (sortColumn is not null)
        {
            sortColumn.IsDefaultSort = true;
            sortColumn.DefaultSortDirection = _gridSettings?.SortDescending == true
                ? SortDirection.Descending
                : SortDirection.Ascending;
        }

        _columns = columns;
        _filterColumns = _columns.Where(IsFilterable).ToList();

        GridTemplateColumns = string.Join(" ", columns.Select(ColumnWidth)) + " min-content"; // + Actions
    }

    /// <summary>Свойство ответа для сортировки: у мета-колонок и без настройки — дата создания</summary>
    static string SortProperty(GridColumn? column)
    {
        if (column is not { IsSystem: true }) return nameof(PostListItemResponse.CreatedAt);

        return column.Key switch
        {
            SystemFieldsCatalog.Title => nameof(PostListItemResponse.Title),
            SystemFieldsCatalog.Categories => nameof(PostListItemResponse.Categories),
            SystemFieldsCatalog.Status => nameof(PostListItemResponse.Status),
            SystemFieldsCatalog.Author => nameof(PostListItemResponse.Author),
            _ => nameof(PostListItemResponse.CreatedAt),
        };
    }

    static string ColumnWidth(GridColumn column)
        => column.Key switch
        {
            SystemFieldsCatalog.Title when column.IsSystem => "3fr",
            SystemFieldsCatalog.Categories when column.IsSystem => "2fr",
            _ => "min-content",
        };

    static string? MetaDisplay(PostListItemResponse item, string key)
        => item.MetaColumns is not null && item.MetaColumns.TryGetValue(key, out var value) ? value : null;

    bool IsImageMetaColumn(string key)
        => _metaFields.FirstOrDefault(f => f.Key == key)?.Type == MetaFieldType.Image;

    static string? FirstOf(string? display)
        => display?.Split(", ").FirstOrDefault();

    void OpenSettingsDialog()
    {
        _gridDraft = _gridSettings;
        _settingsDialogVisible = true;
    }

    void CancelSettingsDialog()
    {
        _settingsDialogVisible = false;
    }

    async Task SaveSettingsAsync()
    {
        await client.PostType.UpdatePresentation(new UpdatePostTypePresentationRequest
        {
            Id = PostType.Id,
            ListViewTemplate = PostType.Presentation.ListViewTemplate ?? "",
            Grid = _gridDraft,
        });

        _gridSettings = _gridDraft;
        _settingsDialogVisible = false;

        RebuildColumns();
        BuildDataProvider();
        // пересоздаём грид — применяются новые колонки и сортировка по умолчанию
        _gridVersion++;

        // Presentation входит в начальные данные сайта — обновляем, как после сохранения типа
        _ = viewModelService.TryUpdateInitialSiteData(forceRemote: true, devAdminPageData: true);
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    async Task Delete(Guid id)
    {
        await client.Post.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }

    public void Refresh()
    {
        table?.RefreshDataAsync();
    }

    void HandleCategoryFilterChanged()
    {
        table.RefreshDataAsync();
    }

    void ClickPostItemCategory(Guid categoryId)
    {
        _filterCategoryId = categoryId;
        StateHasChanged();
        HandleCategoryFilterChanged();
    }

    void OnPostListChanged(string postType)
    {
        if (PostType is null) return;
        if (!string.IsNullOrEmpty(postType)
            && !string.Equals(postType, PostType.TypeName, StringComparison.OrdinalIgnoreCase))
            return;

        Refresh();
    }

    public void Dispose()
    {
        clientHub.OnPostListChanged -= OnPostListChanged;
    }

    /// <summary>Колонка грида: дескриптор из настройки презентации типа + состояние сортировки</summary>
    public sealed class GridColumn(PostTypeGridColumnInfo info)
    {
        public string Key => info.Key;
        public string Title => info.Title;

        /// <summary>Базовая колонка из каталога системных слотов (не мета-поле)</summary>
        public bool IsSystem => info.IsSystem;

        public bool IsDefaultSort { get; set; }
        public SortDirection DefaultSortDirection { get; set; } = SortDirection.Descending;
    }

    #region FILTERS
    /// <summary>Показать/скрыть панель фильтров. При скрытии панель разбирается
    /// вместе с состоянием — фильтры перестают применяться.</summary>
    void ToggleFilters()
    {
        _filtersVisible = !_filtersVisible;

        if (!_filtersVisible)
            table.RefreshDataAsync();
    }

    /// <summary>Колонка, для которой есть фильтр в панели (для категорий фильтра нет)</summary>
    bool IsFilterable(GridColumn column)
    {
        if (!column.IsSystem) return true;

        return column.Key switch
        {
            SystemFieldsCatalog.Title or SystemFieldsCatalog.Author or SystemFieldsCatalog.CreatedAt => true,
            SystemFieldsCatalog.Status => PostType.EnabledFeatures.Contains(PostTypeConstants.Features.Status),
            _ => false,
        };
    }
    #endregion
}
