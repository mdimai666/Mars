using System.Collections.ObjectModel;
using Mars.Admin.Framework.Extensions;
using Mars.Admin.Framework.Hub;
using Mars.Contracts.MetaFields;
using Mars.Contracts.Posts;
using Mars.Contracts.PostTypes;
using Mars.Contracts.Interfaces;
using Mars.Contracts.Resources;
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
                    sortColumn = def?.Kind switch
                    {
                        GridColumnKind.Title => nameof(PostListItemResponse.Title),
                        GridColumnKind.Categories => nameof(PostListItemResponse.Categories),
                        GridColumnKind.Status => nameof(PostListItemResponse.Status),
                        GridColumnKind.Author => nameof(PostListItemResponse.Author),
                        _ => nameof(PostListItemResponse.CreatedAt),
                    };
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
                    MetaFields = _columns.Where(c => c.Kind == GridColumnKind.Meta).Select(c => c.Key).ToArray(),
                    Filters = _filtersPanel?.BuildGridFilters() ?? [],
                });

                var collection = new Collection<PostListItemResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    /// <summary>Доступные колонки: базовые (с учётом фич типа) + мета-поля</summary>
    List<GridColumn> BuildAvailableColumns()
    {
        var list = new List<GridColumn>
        {
            new(PostTypeGridConstants.Title, AppRes.Title, GridColumnKind.Title),
        };

        if (PostType.EnabledFeatures.Contains(PostTypeConstants.Features.Category))
            list.Add(new GridColumn(PostTypeGridConstants.Categories, AppRes.Categories, GridColumnKind.Categories));

        if (PostType.EnabledFeatures.Contains(PostTypeConstants.Features.Status))
            list.Add(new GridColumn(PostTypeGridConstants.Status, AppRes.Status, GridColumnKind.Status));

        list.Add(new GridColumn(PostTypeGridConstants.Author, AppRes.Author, GridColumnKind.Author));
        list.Add(new GridColumn(PostTypeGridConstants.CreatedAt, AppRes.CreatedAt, GridColumnKind.CreatedAt));

        foreach (var field in _metaFields)
        {
            // плоскому гриду не подходят многовариантные и вычислимые поля
            if (field.Type is MetaFieldType.Query or MetaFieldType.SelectMany) continue;
            list.Add(new GridColumn(field.Key, field.Title, GridColumnKind.Meta));
        }

        return list;
    }

    void RebuildColumns()
    {
        var available = BuildAvailableColumns();
        var configured = _gridSettings?.Columns ?? [];
        var configuredKeys = configured.Select(c => c.Key).ToHashSet();
        var columns = new List<GridColumn>();

        foreach (var conf in configured)
        {
            if (!conf.Visible) continue;
            var col = available.FirstOrDefault(c => c.Key == conf.Key);
            if (col is null) continue;
            columns.Add(col);
            available.Remove(col);
        }

        // колонки, которых нет в настройке, — в конце; скрытые настройкой не добавляются
        columns.AddRange(available.Where(c => !configuredKeys.Contains(c.Key)));

        // сортировка по умолчанию — только базовые колонки; запасная — дата создания
        var sortKey = _gridSettings?.SortKey;
        var sortColumn = columns.FirstOrDefault(c => c.Key == sortKey && c.Kind != GridColumnKind.Meta)
                         ?? columns.FirstOrDefault(c => c.Kind == GridColumnKind.CreatedAt);
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

    static string ColumnWidth(GridColumn column)
        => column.Kind switch
        {
            GridColumnKind.Title => "3fr",
            GridColumnKind.Categories => "2fr",
            GridColumnKind.Meta => "min-content",
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

    public enum GridColumnKind
    {
        Title,
        Categories,
        Status,
        Author,
        CreatedAt,
        Meta,
    }

    public class GridColumn
    {
        public GridColumn(string key, string title, GridColumnKind kind)
        {
            Key = key;
            Title = title;
            Kind = kind;
        }

        public string Key { get; }
        public string Title { get; }
        public GridColumnKind Kind { get; }
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

    /// <summary>Колонка, для которой есть фильтр в панели</summary>
    bool IsFilterable(GridColumn column)
        => column.Kind switch
        {
            GridColumnKind.Title or GridColumnKind.Author or GridColumnKind.CreatedAt => true,
            GridColumnKind.Status => PostType.EnabledFeatures.Contains(PostTypeConstants.Features.Status),
            GridColumnKind.Meta => true,
            _ => false,
        };
    #endregion
}
