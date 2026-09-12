using Mars.Forms.Contracts;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Ряд-уровень зоны или контейнера в дизайнере: узлы идут ячейками одного ряда, а пустая
/// область принимает бросок «в конец».
/// </summary>
public partial class FormLayoutRoot
{
    [CascadingParameter] FormLayoutEditor Editor { get; set; } = default!;

    /// <summary>Зона, в которой лежит область</summary>
    [Parameter, EditorRequired] public string Zone { get; set; } = "";

    /// <summary>Узлы области в порядке отображения</summary>
    [Parameter, EditorRequired] public IReadOnlyList<FormLayoutNode> Nodes { get; set; } = [];

    /// <summary>Родительский узел: контейнер; null — корень зоны</summary>
    [Parameter] public FormItem? Parent { get; set; }

    FormLayoutDraft Draft => Editor.Draft;

    FormLayoutEditor.LayoutDropTarget Target() => new(Zone, Parent?.Key, null);

    Task AddRowAsync() => AddAsync(FormItemKind.Row);

    async Task AddAsync(FormItemKind kind)
    {
        if (Draft.Add(kind, Parent?.Key, Parent is null ? Zone : null) is not null) await Editor.EmitAsync();
    }
}
