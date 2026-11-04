using Mars.Admin.Framework.Components.Forms;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.SiteSpace.Pages.UserViews;

public partial class ProfileEditPage
{
    [Inject] NavigationManager navigationManager { get; set; } = default!;

    MetaValuesForm? _metaValueForm;

    Task OnBeforeSave(ProfileEditModel model)
        => _metaValueForm is null ? Task.CompletedTask : _metaValueForm.CommitAllAsync();

    void OnAfterSave(ProfileEditModel model)
    {
        navigationManager.NavigateTo("/dev/Profile");
    }
}
