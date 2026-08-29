using Mars.Cms.Contracts.MetaFields;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.PostTypeViews;

public partial class EditPostTypePresentationPage
{
    IReadOnlyCollection<MetaFieldDetailResponse> _metaFields = [];

    protected override async Task OnInitializedAsync()
    {
        // мета-поля нужны редактору колонок грида
        var detail = await _client.PostType.Get(ID);
        _metaFields = detail?.MetaFields ?? [];
    }

    [Inject] protected IMarsWebApiClient _client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] NavigationManager _navigationManager { get; set; } = default!;
    [Inject] ViewModelService _viewModelService { get; set; } = default!;
    [Inject] IActAppService _actAppService { get; set; } = default!;

    [Parameter] public Guid ID { get; set; }

    StandardEditContainer<PostTypePresentationEditModel> f = default!;

    void AfterSave()
    {
        _ = _viewModelService.TryUpdateInitialSiteData(forceRemote: true, devAdminPageData: true);
    }

    private async Task HandleOnMenuListViewTemplateChanged(MenuChangeEventArgs args)
    {
        if (args.Id == "open_presentation_template")
        {
            // шаблон списка живёт в специальном фронте админки (data/admin/front)
            _navigationManager.NavigateTo("front/editor/admin");
            return;
        }
        else if (args.Id == "create_presentation_template")
        {
            var commandId = "mars.content.templates.createPresentation";
            var xresult = await _actAppService.Inject(commandId, new Dictionary<string, string>
            {
                ["postTypeName"] = f.Model.PostType.TypeName,
            });
            //TODO: как то коряво, пересмотреть XActions

            if (xresult.Ok)
            {
                var postTypeName = f.Model.PostType.TypeName;
                var fileRelPath = $"postTypes/{postTypeName}/listView.hbs";

                f.Model.ListViewTemplate = fileRelPath;
                var saved = await f.Save();

                if (!saved)
                {
                    _ = _messageService.Warning("Не удалось сохранить");
                }

                _navigationManager.NavigateTo("front/editor/admin");
            }
            return;
        }
        throw new NotImplementedException($"id '{args.Id}' is not implement");
    }
}
