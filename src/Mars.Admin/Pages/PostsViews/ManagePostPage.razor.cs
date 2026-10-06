using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Common;
using Mars.Media.Contracts.Files;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Pages.PostsViews;

public partial class ManagePostPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    ManagePostView _managePostView = default!;

    string urlEditPage => "/dev/EditPost";
    string query => $"?posttype={POSTTYPE}";

    PostTypeAdminPanelItemResponse postType = Q.Site.PostTypes.First(s => s.TypeName == "post");

    [Parameter]
    public string POSTTYPE { get; set; } = "post";

    string prevPostType = "";

    bool _isSingle;
    Guid _singlePostId;

    int? _totalCount;
    int? _categoriesCount;
    int? _mediaCount;
    int? _statusesCount;

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

            //_managePostView.Refresh();
        }
    }

    async Task LoadKpi()
    {
        try
        {
            var posts = await client.Post.List(postType.TypeName, new ListPostQueryRequest { Take = 1, Sort = "-CreatedAt" });
            _totalCount = posts.TotalCount;

            var cats = await client.PostCategory.ListForPostType(postType.TypeName, new() { Take = 1, Sort = "-CreatedAt" });
            _categoriesCount = cats.TotalCount;

            var media = await client.Media.List(new ListFileQueryRequest { Take = 1, Sort = "-CreatedAt" });
            _mediaCount = media.TotalCount;

            var detail = await client.PostType.Get(postType.Id);
            _statusesCount = detail?.PostStatusList?.Count;
        }
        catch
        {
            // KPI справочные: список работает независимо от них
        }
        StateHasChanged();
    }

}
