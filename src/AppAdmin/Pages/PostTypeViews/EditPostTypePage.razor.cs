using AppFront.Shared.Components.MetaFieldViews;
using Mars.Shared.Validators;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
namespace AppAdmin.Pages.PostTypeViews;

public partial class EditPostTypePage
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;
    [Inject] IAppMediaService mediaService { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;

    [Parameter] public Guid ID { get; set; }

    StandartEditContainer<PostTypeEditModel> f = default!;

    bool importButtonDisabled;
    string url = "";
    string import_json = "";
    bool visibleImportModal;

    [ValidateSourceUri]
    string _FormListSetter = "";
    [ValidateSourceUri]
    string _FormEditSetter = "";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        //js = new MyJS(JSRuntime);

        url = Q.ServerUrlJoin($"/api/PostType/PostTypeExport/{ID}");
    }

    void AddNewField()
    {
        int order = f.Model.MetaFields.Any() ? f.Model.MetaFields.Max(s => s.Order) + 1 : 0;
        Guid parentId = Guid.Empty;
        f.Model.MetaFields.Add(FormMetaField.NewField(order, parentId));
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
