using Mars.Identity.Contracts.Roles;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.SiteSpace.Pages.RoleViews;

public partial class CreateRoleDialog
{
    [CascadingParameter] public IDialogInstance Dialog { get; set; } = default!;

    [Parameter] public CreateRoleModel Content { get; set; } = default!;

    RoleDetailResponse? _created;

    async Task<CreateRoleModel> SaveAction(CreateRoleModel model, bool isNew)
    {
        _created = await client.Role.Create(new CreateRoleRequest { Name = model.Name.Trim() });
        return model;
    }

    Task AfterSave(CreateRoleModel model) => Dialog.CloseAsync(_created);
}
