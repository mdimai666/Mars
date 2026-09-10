using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Хост формы значений метаполей владельца: держит реестр тяжёлых редакторов (WYSIWYG, код,
/// блочный редактор пишут значение по <see cref="PullAsync"/> перед сохранением, а не на ввод)
/// и отдаёт рендереру общего слоя дерево провайдера вместе с каскадами EAV-строк.
/// </summary>
public partial class MetaValuesForm : IHeavyMetaValueEditors
{
    [Parameter, EditorRequired] public FormDefinition Definition { get; set; } = default!;

    /// <summary>Значения владельца (EAV-строки) — редакторы метаполей берут их из каскада</summary>
    [Parameter, EditorRequired] public List<MetaValueEditModel> MetaValues { get; set; } = default!;

    /// <summary>Определения полей владельца — доменные редакторы берут настройку поля из каскада</summary>
    [Parameter, EditorRequired] public List<MetaFieldEditModel> MetaFields { get; set; } = default!;

    /// <summary>true — рендер для конечного пользователя (скрытые поля не показываются)</summary>
    [Parameter] public bool Client { get; set; }

    readonly HeavyMetaValueEditorRegistry _editors = new();

    FormValuesModel? _store;

    /// <summary>Мешок формы пустой: значения метаполей живут в строках владельца</summary>
    IFormValueStore Store
        => _store ??= new FormValuesModel(new FormValues { OwnerModel = Definition?.OwnerModel ?? "" });

    /// <summary>Реестр тяжёлых редакторов — уходит каскадом в редакторы значений</summary>
    public IHeavyMetaValueEditors Editors => _editors;

    public void RegisterHeavyEditor(IHeavyMetaValueEditor editor) => _editors.RegisterHeavyEditor(editor);

    public void UnregisterHeavyEditor(IHeavyMetaValueEditor editor) => _editors.UnregisterHeavyEditor(editor);

    /// <summary>Забрать значения из всех тяжёлых редакторов в модель — вызывать перед сохранением формы</summary>
    public Task PullAsync() => _editors.PullAsync();
}
