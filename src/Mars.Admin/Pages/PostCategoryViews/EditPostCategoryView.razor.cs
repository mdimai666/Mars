using Mars.Admin.Pages.PostCategoriesViews;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Core.Features;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Pages.PostCategoryViews;

public partial class EditPostCategoryView
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;

    [Parameter, EditorRequired] public Guid ID { get; set; }
    [Parameter, EditorRequired] public string PostCategoryTypeName { get; set; } = default!;
    [Parameter, EditorRequired] public string PostTypeName { get; set; } = default!;

    [Parameter] public EventCallback<PostCategoryEditModel> OnLoadData { get; set; }
    [Parameter] public EventCallback<PostCategoryEditModel> BeforeSave { get; set; }
    [Parameter] public EventCallback<PostCategoryEditModel> AfterSave { get; set; }
    [Parameter] public EventCallback AfterDelete { get; set; }

    StandardEditForm1<PostCategoryEditModel> _editForm1 = default!;
    FormMetaValue? metaValueForm;

    async Task OnBeforeSave(PostCategoryEditModel model)
    {
        if (metaValueForm is not null) await metaValueForm.PullAsync();
        await BeforeSave.InvokeAsync(model);
    }

    void OnChangeTitle()
    {
        if (string.IsNullOrWhiteSpace(_editForm1.Model.Slug) || Guid.TryParse(_editForm1.Model.Slug, out Guid _))
        {
            _editForm1.Model.Slug = TextTool.TranslateToPostSlug(_editForm1.Model.Title);
        }
    }

    public async Task Load()
    {
        StateHasChanged();
        await Task.Delay(100);
        await _editForm1.Load();
    }
}
