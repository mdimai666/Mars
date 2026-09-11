using Mars.Forms.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>Компактная кнопка-иконка дизайнера раскладки: действие подписано в title</summary>
public partial class FormLayoutIconButton
{
    [Parameter, EditorRequired] public Icon Icon { get; set; } = default!;

    [Parameter] public string? Title { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
}
