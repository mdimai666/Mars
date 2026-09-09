using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

public partial class FormMetaValue : IHeavyMetaValueEditors
{
    [CascadingParameter] List<MetaValueEditModel> MetaValues { get; set; } = default!;

    [CascadingParameter] List<MetaFieldEditModel> MetaFields { get; set; } = default!;

    [Parameter] public bool Vertical { get; set; }
    [Parameter] public bool Client { get; set; }

    readonly HeavyMetaValueEditorRegistry _editors = new();

    /// <summary>Реестр тяжёлых редакторов — уходит каскадом в редакторы значений</summary>
    public IHeavyMetaValueEditors Editors => _editors;

    /// <summary>Регистрация тяжёлого редактора (обёртки регистрируются сами)</summary>
    public void RegisterHeavyEditor(IHeavyMetaValueEditor editor) => _editors.RegisterHeavyEditor(editor);

    public void UnregisterHeavyEditor(IHeavyMetaValueEditor editor) => _editors.UnregisterHeavyEditor(editor);

    /// <summary>Забрать значения из всех тяжёлых редакторов в модель —
    /// вызывать перед сохранением формы (тяжёлые редакторы не пушат значения при вводе)</summary>
    public Task PullAsync() => _editors.PullAsync();
}
