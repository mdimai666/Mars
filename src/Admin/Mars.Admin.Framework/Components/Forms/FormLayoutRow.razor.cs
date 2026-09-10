using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Строка дизайнера раскладки формы: один элемент дерева (поле или секция) и его дети.
/// Все изменения уходят в <see cref="FormLayoutEditor"/>, который собирает раскладку.
/// </summary>
public partial class FormLayoutRow
{
    [CascadingParameter] FormLayoutEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public FormLayoutEditor.LayoutRow Row { get; set; } = default!;

    /// <summary>Глубина от корня зоны: дети секции рисуются с отступом</summary>
    [Parameter] public int Depth { get; set; }

    string RowStyle => Depth > 0 ? $"margin-left:{Depth * 20}px" : "";

    Task ToggleVisibleAsync(bool visible)
    {
        Row.Visible = visible;
        return Editor.EmitAsync();
    }

    Task ToggleCollapsedAsync(bool collapsed)
    {
        Row.Collapsed = collapsed;
        return Editor.EmitAsync();
    }

    Task SetTitleAsync(string title)
    {
        Row.Title = title;
        return Editor.EmitAsync();
    }
}
