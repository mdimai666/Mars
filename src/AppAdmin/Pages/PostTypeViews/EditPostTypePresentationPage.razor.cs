using Mars.Shared.Models;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppAdmin.Pages.PostTypeViews;

public partial class EditPostTypePresentationPage
{
    [Inject] protected IMarsWebApiClient _client { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] NavigationManager _navigationManager { get; set; } = default!;
    [Inject] ViewModelService _viewModelService { get; set; } = default!;

    [Parameter] public Guid ID { get; set; }

    StandartEditContainer<PostTypePresentationEditModel> f = default!;

    void AfterSave()
    {
        _ = _viewModelService.TryUpdateInitialSiteData(forceRemote: true, devAdminPageData: true);
    }

    private async Task HandleOnMenuListViewTemplateChanged(MenuChangeEventArgs args)
    {
        if (args.Id == "open_presentation_template")
        {
            try
            {
                SourceUri sourceUri = f.Model.ListViewTemplate;
                var readPost = await _client.Post.GetBySlug(sourceUri[1], sourceUri[0], renderContent: false);
                var link = $"EditPost/{sourceUri[0]}/{readPost.Id}";
                _navigationManager.NavigateTo(link);
            }
            catch
            {
                _ = _messageService.Error("invalid sourceUri");
            }
        }
        else
        {
            throw new NotImplementedException($"id '{args.Id}' is not implement");
        }
    }
}
