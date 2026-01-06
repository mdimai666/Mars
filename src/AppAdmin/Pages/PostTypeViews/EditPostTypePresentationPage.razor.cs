using Mars.Shared.Interfaces;
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
    [Inject] IActAppService _actAppService { get; set; } = default!;

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
        else if (args.Id == "create_presentation_template")
        {
            var commandId = "Mars.XActions.Content.Templates.CreatePostTypePresentationTemplateAct";
            var xresult = await _actAppService.Inject(commandId, [f.Model.PostType.TypeName]);
            //TODO: как то коряво, пересмотреть XActions

            if (xresult.Ok)
            {
                var postTypeName = f.Model.PostType.TypeName;
                var postSlug = $"admin_list_{postTypeName}_page";
                var containerTypeName = "template";

                f.Model.ListViewTemplate = $"/{containerTypeName}/{postSlug}";
                var saved = await f.Save();

                if (!saved)
                {
                    _ = _messageService.Warning("Не удалось сохранить");
                }

                var readPost = await _client.Post.GetBySlug(postSlug, containerTypeName, renderContent: false);
                var link = $"EditPost/{postTypeName}/{readPost.Id}";
                _navigationManager.NavigateTo(link);
            }
        }
        else
        {
            throw new NotImplementedException($"id '{args.Id}' is not implement");
        }
    }
}
