using Mars.Core.Utils;
using Mars.Identity.Contracts.Roles;
using Mars.Identity.Contracts.UserTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Mars.Admin.Pages.UserViews;

public partial class CreateUserDialog
{
    [CascadingParameter]
    public IDialogInstance Dialog { get; set; } = default!;

    /// <summary>Данные диалога (передаёт шим ShowDialogAsync параметром "Content").</summary>
    [Parameter]
    public CreateUserEditFormData Content { get; set; } = default!;

    CreateUserModel model => Content.Model;

    IEnumerable<RoleSummaryResponse> _selRoles = [];
    IEnumerable<RoleSummaryResponse> SelRoles
    {
        get => _selRoles;
        set
        {
            _selRoles = value;
            Content.Model.Roles = _selRoles.Select(s => s.Name).ToList();
        }
    }

    UserTypeListItemResponse SelUserType
    {
        get => Content.UserTypes.FirstOrDefault(s => s.TypeName == Content.Model.Type) ?? default!;
        set
        {
            Content.Model.Type = value.TypeName ?? "";
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Content.DefaultCreateRole is not null)
        {
            _selRoles = [Content.DefaultCreateRole];
        }
    }

    Task AfterSave(CreateUserModel model) => Dialog.CloseAsync(model);

    TextInputType passwordFieldType = TextInputType.Password;
    static Icon eyeShow = new Icons.Regular.Size16.Eye();
    static Icon eyeOff = new Icons.Regular.Size16.EyeOff();
    Icon passwordShowButtonIcon => passwordFieldType == TextInputType.Password ? eyeShow : eyeOff;

    void GeneratePassword()
    {
        model.Password = Password.Generate(8, 2);
        passwordFieldType = TextInputType.Text;
    }

    void TogglePassword()
    {
        passwordFieldType = passwordFieldType == TextInputType.Password ? TextInputType.Text : TextInputType.Password;
    }
}
