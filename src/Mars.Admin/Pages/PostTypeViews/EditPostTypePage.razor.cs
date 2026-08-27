using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.PostTypeViews;

public partial class EditPostTypePage
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;
    [Inject] IAppMediaService mediaService { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;

    [Parameter] public Guid ID { get; set; }

    StandartEditContainer<PostTypeEditModel> f = default!;

    bool importButtonDisabled;
    string url = "";
    string import_json = "";
    bool visibleImportModal;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        url = Q.ServerUrlJoin($"/api/PostType/PostTypeExport/{ID}");
    }

    void AddNewField()
    {
        int order = f.Model.MetaFields.Any() ? f.Model.MetaFields.Max(s => s.Order) + 1 : 0;
        f.Model.MetaFields.Add(FormMetaField.NewField(order));
    }

    async Task OnToggleFeatureAsync(PostTypeEditModel context, string feature, bool enabled)
    {
        context.ToggleFeature(feature, enabled);

        if (feature != PostTypeConstants.Features.PostImage || !enabled) return;

        var candidates = context.ImagePointerCandidates();
        if (candidates.Count == 0)
        {
            // картинок в типе ещё нет — поле создаётся сразу
            var created = context.CreateFeatureImageField();
            context.ImageFieldKey = created.Key;
            return;
        }

        // картинки уже есть — выбрать: создать новое поле или взять существующее
        DialogParameters parameters = new()
        {
            Title = "Поле картинки поста",
            SecondaryAction = null,
            Width = "500px",
            Modal = true,
            PreventScroll = true,
        };

        var dialog = await _dialogService.ShowDialogAsync<PostImageSelectDialog>(
            new PostImageSelectDialogData
            {
                Options = candidates.Select(s => (s.Key, s.Title)).ToList(),
            },
            parameters);
        var result = await dialog.Result;

        if (result.Cancelled || result.Data is not string choice) return;

        if (choice == PostImageSelectDialog.CreateNewMarker)
        {
            var created = context.CreateFeatureImageField();
            context.ImageFieldKey = created.Key;
        }
        else
        {
            context.ImageFieldKey = choice;
        }
    }

    void AfterSave()
    {
        _ = viewModelService.TryUpdateInitialSiteData(forceRemote: true, devAdminPageData: true);
    }

    void AfterDelete()
    {
        _ = viewModelService.TryUpdateInitialSiteData(forceRemote: true, devAdminPageData: true);
    }

    void ShowImportModal()
    {
        visibleImportModal = true;
    }

    void ImportModalOnCancel()
    {
        visibleImportModal = false;
    }

    private async void LoadFiles(InputFileChangeEventArgs e)
    {
        importButtonDisabled = true;
        StateHasChanged();

        using MemoryStream ms = new();
        await e.File.OpenReadStream().CopyToAsync(ms);
        var bytes = ms.ToArray();
        string json = System.Text.Encoding.UTF8.GetString(bytes);
        import_json = json;
        //importVal = JsonConvert.DeserializeObject<SystemImportSettingsFile_v1>(import_json);

        importButtonDisabled = false;
        StateHasChanged();

    }

    void OnImportClick()
    {
        ////importVal = JsonConvert.DeserializeObject<SystemImportSettingsFile_v1>(import_json);
        ////var result = await viewModelService.SystemImportSettings(importVal);
        //string asPostType = f!.Model.TypeName;
        //var result = await f!.service.PostTypeImport(import_json, asPostType);

        //if (result.Ok)
        //{
        //    _ = messageService.Success(result.Message);
        //    _ = f!.Load();
        //}
        //else
        //{
        //    _ = messageService.Error(result.Message);
        //}
        throw new NotImplementedException();
    }
}
