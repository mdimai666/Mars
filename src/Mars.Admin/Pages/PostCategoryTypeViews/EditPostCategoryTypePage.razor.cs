using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Pages.PostCategoryTypeViews;

public partial class EditPostCategoryTypePage
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;
    [Inject] IAppMediaService mediaService { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;

    [Parameter] public Guid ID { get; set; }

    StandardEditContainer<PostCategoryTypeEditModel> f = default!;

}
