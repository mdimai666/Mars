using Mars.Shared.Contracts.PostCategories;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.Shared.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppAdmin.Pages.PostCategoryViews;

public partial class ManagePostCategoryPage
{
    [Inject] IMarsWebApiClient _client { get; set; } = default!;

    [Parameter]
    public string POSTTYPE { get; set; } = "post";

    ManagePostCategoryView _managePostCategoryView = default!;
    EditPostCategoryView _editPostForm = default!;
    string urlEditPage => "/dev/EditPostCategory";
    string query => $"?posttype={POSTTYPE}";

    //PostCategoryTypeDetailResponse? postCategoryType;
    PostTypeAdminPanelItemResponse postType = Q.Site.PostTypes.First(s => s.TypeName == "post");

    string prevPostType = "";
    bool _busy;
    Guid _selId;
    bool showEditForm;

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(POSTTYPE) == false && prevPostType != POSTTYPE)
        {
            prevPostType = POSTTYPE;
            //Load();
            postType = Q.Site.PostTypes.FirstOrDefault(s => s.TypeName == POSTTYPE) ?? Q.Site.PostTypes.First(s => s.TypeName == "post");
        }
    }

    //async void Load()
    //{
    //    _busy = true;
    //    StateHasChanged();

    //    postCategoryType = await _client.PostCategoryType.GetByName(POSTTYPE);

    //    _busy = false;
    //    StateHasChanged();
    //}

    void OnClickItem(PostCategoryListItemResponse item)
    {
        _selId = item.Id;
        showEditForm = true;
        _ = _editPostForm.Load();
    }

    void OnClickCreate()
    {
        _selId = Guid.Empty;
        showEditForm = true;
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
