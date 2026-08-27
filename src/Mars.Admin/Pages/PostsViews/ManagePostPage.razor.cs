using Mars.Shared.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Pages.PostsViews;

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

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(POSTTYPE) == false && prevPostType != POSTTYPE)
        {
            prevPostType = POSTTYPE;
            postType = Q.Site.PostTypes.FirstOrDefault(s => s.TypeName == POSTTYPE) ?? Q.Site.PostTypes.First(s => s.TypeName == "post");

            _isSingle = postType.EnabledFeatures.Contains(PostTypeConstants.Features.Single);
            _singlePostId = Guid.Empty;
            if (_isSingle)
            {
                var single = await client.Post.Single(postType.TypeName);
                _singlePostId = single.Id;
            }

            //_managePostView.Refresh();
        }
    }

}
