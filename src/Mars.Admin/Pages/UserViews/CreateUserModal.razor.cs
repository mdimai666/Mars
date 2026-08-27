using Mars.Core.Utils;
using Mars.Shared.Contracts.Roles;
using Mars.Shared.Contracts.UserTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace AppAdmin.Pages.UserViews;

public partial class CreateUserModal
{
    [Parameter]
    public CreateUserEditFormData Content { get; set; } = default!;

    StandartEditForm1<CreateUserModel> _editForm1 = default!;

    bool _visible;
    [Parameter]
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value) return;
            _visible = value;
            _ = VisibleChanged.InvokeAsync(_visible);
        }
    }

    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public EventCallback<CreateUserModel> AfterCreate { get; set; }

    FluentDialog Dialog { get; set; } = default!;

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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Content.DefaultCreateRole is not null)
        {
            _selRoles = [Content.DefaultCreateRole];
        }

        if (_editForm1 is not null)
        {
            _editForm1.Model = model;
        }
    }

    void OnDialogResult(DialogResult result)
    {
        if (result.Cancelled)
        {
            Visible = false;
            return;
        }
    }

    void AfterSave(CreateUserModel model)
    {
        Visible = false;
        AfterCreate.InvokeAsync(model);
    }

    TextFieldType passwordFieldType = TextFieldType.Password;
    static Icon eyeShow = new Icons.Regular.Size16.Eye();
    static Icon eyeOff = new Icons.Regular.Size16.EyeOff();
    Icon passwordShowButtonIcon => passwordFieldType == TextFieldType.Password ? eyeShow : eyeOff;

    void GeneratePassword()
    {
        model.Password = Password.Generate(8, 2);
        passwordFieldType = TextFieldType.Text;
    }

    void TogglePassword()
    {
        passwordFieldType = passwordFieldType == TextFieldType.Password ? TextFieldType.Text : TextFieldType.Password;
    }
}
