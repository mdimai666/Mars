using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Contracts.Validators;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Mars.Admin.Pages.PostCategoryTypeViews;

public partial class EditPostCategoryTypePage
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;
    [Inject] IAppMediaService mediaService { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;

    [Parameter] public Guid ID { get; set; }

    StandartEditContainer<PostCategoryTypeEditModel> f = default!;

    void AddNewField()
    {
        int order = f.Model.MetaFields.Any() ? f.Model.MetaFields.Max(s => s.Order) + 1 : 0;
        f.Model.MetaFields.Add(FormMetaField.NewField(order));
    }

}
