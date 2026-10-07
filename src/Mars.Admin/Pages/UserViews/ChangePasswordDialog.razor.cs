using Mars.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Mars.Admin.Pages.UserViews;

public partial class ChangePasswordDialog
{
    [CascadingParameter]
    public IDialogInstance Dialog { get; set; } = default!;

    /// <summary>Данные диалога (передаёт шим ShowDialogAsync параметром "Content").</summary>
    [Parameter]
    public ChangePasswordModel Content { get; set; } = default!;

    ChangePasswordModel model => Content;

    TextInputType passwordFieldType = TextInputType.Password;
    static Icon eyeShow = new Icons.Regular.Size16.Eye();
    static Icon eyeOff = new Icons.Regular.Size16.EyeOff();
    Icon passwordShowButtonIcon => passwordFieldType == TextInputType.Password ? eyeShow : eyeOff;

    Task AfterSave(ChangePasswordModel model) => Dialog.CloseAsync(model);

    void GeneratePassword()
    {
        model.NewPassword = Password.Generate(8, 2);
        passwordFieldType = TextInputType.Text;
    }

    void TogglePassword()
    {
        passwordFieldType = passwordFieldType == TextInputType.Password ? TextInputType.Text : TextInputType.Password;
    }
}
