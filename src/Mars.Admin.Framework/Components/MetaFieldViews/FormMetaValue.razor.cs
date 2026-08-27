using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

public partial class FormMetaValue
{
    [CascadingParameter] List<MetaValueEditModel> MetaValues { get; set; } = default!;

    [CascadingParameter] List<MetaFieldEditModel> MetaFields { get; set; } = default!;

    [Parameter] public bool Vertical { get; set; }
    [Parameter] public bool Client { get; set; }

    readonly List<IHeavyMetaValueEditor> _heavyEditors = [];

    /// <summary>Регистрация тяжёлого редактора (обёртки регистрируются сами)</summary>
    public void RegisterHeavyEditor(IHeavyMetaValueEditor editor)
    {
        if (!_heavyEditors.Contains(editor)) _heavyEditors.Add(editor);
    }

    public void UnregisterHeavyEditor(IHeavyMetaValueEditor editor) => _heavyEditors.Remove(editor);

    /// <summary>Забрать значения из всех тяжёлых редакторов в модель —
    /// вызывать перед сохранением формы (тяжёлые редакторы не пушат значения при вводе)</summary>
    public async Task PullAsync()
    {
        foreach (var editor in _heavyEditors.ToArray())
            await editor.CommitAsync();
    }
}
