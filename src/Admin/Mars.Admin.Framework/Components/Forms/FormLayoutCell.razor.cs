using Mars.Forms.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Узел сетки в дизайнере: прямоугольник ряда, ячейки или элемента со своим содержимым.
/// Все изменения уходят в <see cref="FormLayoutEditor"/>, который собирает раскладку через черновик.
/// </summary>
public partial class FormLayoutCell
{
    [CascadingParameter] FormLayoutEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public FormLayoutNode Node { get; set; } = default!;

    FormLayoutDraft Draft => Editor.Draft;

    /// <summary>Ячейка сетки: у колонки — её доля из 12, у ряда и элемента — во всю ширину</summary>
    string CellStyle => Node.Item.Kind == FormItemKind.Column ? FormLayoutEditor.FlexStyle(Node.Item.Width) : "";

    /// <summary>Классы видимой части: ряд и колонка — блок во всю ячейку, элемент — компактная строка</summary>
    string BoxClass => Node.Item.Kind == FormItemKind.Column ? "h-100" : "";

    /// <summary>Рамка по типу узла: у ряда и у колонки цвета разные, элемент — прямоугольник с названием</summary>
    string BoxStyle => Node.Item.Kind switch
    {
        FormItemKind.Row => FormLayoutEditor.RowStyle,
        FormItemKind.Column => $"{FormLayoutEditor.ColumnStyle}; margin:2px",
        FormItemKind.Container => "",
        _ => FormLayoutEditor.ElementStyle,
    };

    string Width => Node.Item.Width ?? FormItemWidths.Full;

    Icon VisibilityIcon => Node.Item.Visible
        ? new Icons.Regular.Size16.Eye()
        : new Icons.Regular.Size16.EyeOff();

    string VisibilityTitle => Node.Item.Visible ? "Скрыть поле в форме" : "Показать поле в форме";

    /// <summary>Цель броска на сам узел: вставить перед ним у его родителя</summary>
    FormLayoutEditor.LayoutDropTarget Target()
        => new(Node.Item.Zone ?? "", Node.Item.Parent, Node.Item.Key);

    /// <summary>Цель броска внутрь узла: в конец его детей</summary>
    FormLayoutEditor.LayoutDropTarget InsideTarget()
        => new(Node.Item.Zone ?? "", Node.Item.Key, null);

    Task AddRowAsync() => AddAsync(FormItemKind.Row);

    Task AddHeadingAsync() => AddAsync(FormItemKind.Heading);

    Task AddDividerAsync() => AddAsync(FormItemKind.Divider);

    Task AddColumnAsync() => AddAsync(FormItemKind.Column, FormItemWidths.Half);

    async Task AddAsync(FormItemKind kind, string? width = null)
    {
        if (Draft.Add(kind, Node.Item.Key, null, null, width) is not null) await Editor.EmitAsync();
    }

    async Task RemoveAsync()
    {
        if (Draft.Remove(Node.Item.Key)) await Editor.EmitAsync();
    }

    async Task SetWidthAsync(string width)
    {
        if (Draft.SetWidth(Node.Item.Key, width)) await Editor.EmitAsync();
    }

    async Task SetTitleAsync(string? title)
    {
        if (Draft.SetTitle(Node.Item.Key, title)) await Editor.EmitAsync();
    }

    async Task ToggleVisibleAsync()
    {
        if (Draft.SetVisible(Node.Item.Key, !Node.Item.Visible)) await Editor.EmitAsync();
    }
}
