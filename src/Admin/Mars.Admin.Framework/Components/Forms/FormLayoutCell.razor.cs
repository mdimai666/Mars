using Mars.Forms.Contracts;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Узел сетки в дизайнере: ячейка узла с содержимым по его типу. Все изменения уходят
/// в <see cref="FormLayoutEditor"/>, который собирает раскладку через черновик.
/// </summary>
public partial class FormLayoutCell
{
    [CascadingParameter] FormLayoutEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public FormLayoutNode Node { get; set; } = default!;

    FormLayoutDraft Draft => Editor.Draft;

    /// <summary>Поле прямо в ряду (или в корне зоны) — неявная ячейка со своей шириной</summary>
    bool IsLooseCell => Node.Item.Kind == FormItemKind.Field && ParentKind is not FormItemKind.Column;

    FormItemKind? ParentKind => Draft.Find(Node.Item.Parent ?? "")?.Kind;

    /// <summary>Дизайнер показывает пропорции ячейки как на форме; контейнеры обведены рамкой</summary>
    string CellStyle
    {
        get
        {
            var parts = new List<string>();
            if (Node.Item.Kind == FormItemKind.Column || IsLooseCell) parts.Add(FormLayoutEditor.FlexStyle(Node.Item));
            if (Node.Item.Kind is FormItemKind.Column or FormItemKind.Row)
                parts.Add("border:1px solid var(--neutral-stroke-rest); border-radius:4px");

            return string.Join("; ", parts);
        }
    }

    string Width => Node.Item.Width ?? FormItemWidths.Full;

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

    async Task SetVisibleAsync(bool visible)
    {
        if (Draft.SetVisible(Node.Item.Key, visible)) await Editor.EmitAsync();
    }
}
