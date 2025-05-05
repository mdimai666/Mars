using Mars.Shared.Contracts.PostTypes;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Pages.PostsViews;

public partial class ManagePostPage
{

    ManagePostView _managePostView = default!;

    string urlEditPage => "/dev/EditPost";
    string query => $"?posttype={POSTTYPE}";

    PostTypeSummaryResponse postType = Q.Site.PostTypes.First(s => s.TypeName == "post");

    [Parameter]
    public string POSTTYPE { get; set; } = "post";

    string prevPostType = "";

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(POSTTYPE) == false && prevPostType != POSTTYPE)
        {
            prevPostType = POSTTYPE;
            postType = Q.Site.PostTypes.FirstOrDefault(s => s.TypeName == POSTTYPE) ?? Q.Site.PostTypes.First(s => s.TypeName == "post");

            //_managePostView.Refresh();
        }
    }

}
