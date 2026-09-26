using Mars.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Mars.Admin.Pages.UserViews;

public partial class ChangePasswordModal
{
    [Parameter]
    public ChangePasswordModel Content { get; set; } = default!;

    StandardEditForm1<ChangePasswordModel> _editForm1 = default!;

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

    TextInputType passwordFieldType = TextInputType.Password;
    static Icon eyeShow = new Icons.Regular.Size16.Eye();
    static Icon eyeOff = new Icons.Regular.Size16.EyeOff();
    Icon passwordShowButtonIcon => passwordFieldType == TextInputType.Password ? eyeShow : eyeOff;

    protected override void OnParametersSet()
    {

        if (_editForm1 is not null)
        {
            _editForm1.Model = model;
        }
    }

    // v5: FluentDialog без Hidden/OnDialogResult — показ/скрытие императивно; Visible остаётся источником правды,
    // синхронизация после рендера. OnStateChange(Closed) сбрасывает Visible (бывший OnDialogResult: Cancelled → Visible=false).
    bool _dialogShown;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _dialogShown = await SyncDialogAsync(Dialog, Visible, _dialogShown);
    }

    static async Task<bool> SyncDialogAsync(FluentDialog? dialog, bool visible, bool shown)
    {
        if (dialog is null) return shown;

        if (visible && !shown)
        {
            await dialog.ShowAsync();
            return true;
        }

        if (!visible && shown)
        {
            await dialog.HideAsync();
            return false;
        }

        return shown;
    }

    void AfterSave(ChangePasswordModel model)
    {
        Visible = false;
        AfterChange.InvokeAsync(model);
    }

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
