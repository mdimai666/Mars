using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Строка дизайнера раскладки: лист-поле или маркер секции. Все изменения уходят
/// в <see cref="FormLayoutEditor"/>, который собирает раскладку.
/// </summary>
public partial class FormLayoutRow
{
    [CascadingParameter] FormLayoutEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public FormLayoutEditor.LayoutRow Row { get; set; } = default!;

    /// <summary>Отступ: 1 у полей, идущих за маркером секции</summary>
    [Parameter] public int Depth { get; set; }

    string RowStyle => Depth > 0 ? $"margin-left:{Depth * 20}px" : "";

    Task ToggleVisibleAsync(bool visible)
    {
        Row.Visible = visible;
        return Editor.EmitAsync();
    }

    Task SetTitleAsync(string title)
    {
        Row.Title = title;
        return Editor.EmitAsync();
    }

    Task SetSectionTitleAsync(string title)
        => Editor.SetSectionTitleAsync(Row, title);
}
