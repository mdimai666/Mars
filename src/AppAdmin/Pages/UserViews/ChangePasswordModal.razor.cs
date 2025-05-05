using Mars.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace AppAdmin.Pages.UserViews;

public partial class ChangePasswordModal
{
    [Parameter]
    public ChangePasswordModel Content { get; set; } = default!;

    StandartEditForm1<ChangePasswordModel> _editForm1 = default!;

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
    [Parameter] public EventCallback<ChangePasswordModel> AfterChange { get; set; }

    FluentDialog Dialog { get; set; } = default!;
    ChangePasswordModel model => Content;

    TextFieldType passwordFieldType = TextFieldType.Password;
    static Icon eyeShow = new Icons.Regular.Size16.Eye();
    static Icon eyeOff = new Icons.Regular.Size16.EyeOff();
    Icon passwordShowButtonIcon => passwordFieldType == TextFieldType.Password ? eyeShow : eyeOff;

    protected override void OnParametersSet()
    {

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

    void AfterSave(ChangePasswordModel model)
    {
        Visible = false;
        AfterChange.InvokeAsync(model);
    }

    void GeneratePassword()
    {
        model.NewPassword = Password.Generate(8, 2);
        passwordFieldType = TextFieldType.Text;
    }

    void TogglePassword()
    {
        passwordFieldType = passwordFieldType == TextFieldType.Password ? TextFieldType.Text : TextFieldType.Password;
    }
}
