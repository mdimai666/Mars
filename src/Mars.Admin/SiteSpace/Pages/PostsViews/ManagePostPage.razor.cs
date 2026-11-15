using Mars.Cms.Contracts.PostCategories;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Common;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.SiteSpace.Pages.PostsViews;

public partial class ManagePostPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    PostTypeAdminPanelItemResponse postType = Q.Site.PostTypes.First(s => s.TypeName == "post");

    [Parameter]
    public string POSTTYPE { get; set; } = "post";

    string prevPostType = "";

    bool _isSingle;
    Guid _singlePostId;

    bool CategoriesEnabled => postType.EnabledFeatures.Contains(PostTypeConstants.Features.Category);

    IReadOnlyDictionary<string, KpiResult> _kpi = new Dictionary<string, KpiResult>();

    protected override async Task OnParametersSetAsync()
    {
        // на маршруте "/Post" без сегмента POSTTYPE приходит пустой строкой — нормализуем к "post"
        var typeName = string.IsNullOrEmpty(POSTTYPE) ? "post" : POSTTYPE;
        if (prevPostType != typeName)
        {
            prevPostType = typeName;
            postType = Q.Site.PostTypes.FirstOrDefault(s => s.TypeName == typeName) ?? Q.Site.PostTypes.First(s => s.TypeName == "post");

            _isSingle = postType.EnabledFeatures.Contains(PostTypeConstants.Features.Single);
            _singlePostId = Guid.Empty;
            if (_isSingle)
            {
                var single = await client.Post.Single(postType.TypeName);
                _singlePostId = single.Id;
            }
            else
            {
                await LoadKpi();
            }
        }
    }

    async Task LoadKpi()
    {
        var keys = new List<string> { PostKpiKeys.TotalFor(postType.TypeName) };
        if (CategoriesEnabled)
            keys.Add(PostCategoryKpiKeys.TotalFor(postType.TypeName));

        try
        {
            _kpi = await client.Kpi.Get(keys);
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
}
