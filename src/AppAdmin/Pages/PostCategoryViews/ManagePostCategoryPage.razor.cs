using Mars.Shared.Contracts.PostCategories;
using Mars.Shared.Contracts.PostTypes;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Pages.PostCategoryViews;

public partial class ManagePostCategoryPage
{
    [Parameter]
    public string POSTTYPE { get; set; } = "post";

    ManagePostCategoryView _managePostCategoryView = default!;
    EditPostCategoryView _editPostForm = default!;

    PostTypeAdminPanelItemResponse postType = Q.Site.PostTypes.First(s => s.TypeName == "post");

    public static string GetPageLink(string postType) => $"PostCategory/{postType}";

    string prevPostType = "";
    bool _busy;
    Guid _selId;
    bool showEditForm;

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(POSTTYPE) == false && prevPostType != POSTTYPE)
        {
            prevPostType = POSTTYPE;
            postType = Q.Site.PostTypes.FirstOrDefault(s => s.TypeName == POSTTYPE) ?? Q.Site.PostTypes.First(s => s.TypeName == "post");
        }
    }

    async Task OnClickItem(PostCategoryListItemResponse item)
    {
        _selId = item.Id;
        showEditForm = true;
        await Task.Delay(10);//await init form
        _ = _editPostForm.Load();
    }

    async Task OnClickCreate()
    {
        _selId = Guid.Empty;
        showEditForm = true;
        await Task.Delay(10);//await init form
        _ = _editPostForm.Load();
    }

    void OnDeleteItem(PostCategoryListItemResponse item)
    {
        _selId = Guid.Empty;
        showEditForm = false;
        //_ = _editPostForm.Load();
    }

    void OnClickCancel()
    {
        _selId = Guid.Empty;
        showEditForm = false;
    }

    void AfterSave()
    {
        _managePostCategoryView.Refresh();
    }

    void AfterDelete()
    {
        _selId = Guid.Empty;
        showEditForm = false;
        _managePostCategoryView.Refresh();
    }
}
