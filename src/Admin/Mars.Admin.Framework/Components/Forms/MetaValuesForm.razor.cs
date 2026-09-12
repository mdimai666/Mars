using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Хост формы значений метаполей владельца без типизированной модели: отдаёт рендереру общего слоя
/// дерево провайдера. Редакторы с отложенной записью (WYSIWYG, код, блочный) регистрируются
/// в <see cref="Commits"/>, страница забирает их значения перед сохранением.
/// </summary>
public partial class MetaValuesForm
{
    [Parameter, EditorRequired] public FormDefinition Definition { get; set; } = default!;

    /// <summary>Значения владельца (EAV-строки) — они же значения формы</summary>
    [Parameter, EditorRequired] public List<MetaValueEditModel> MetaValues { get; set; } = default!;

    /// <summary>Определения полей владельца — по ним стор переводит значения (варианты выбора, Id строк)</summary>
    [Parameter, EditorRequired] public List<MetaFieldEditModel> MetaFields { get; set; } = default!;

    /// <summary>true — рендер для конечного пользователя (скрытые поля не показываются)</summary>
    [Parameter] public bool Client { get; set; }

    /// <summary>Хуки отложенной записи — одни на все зоны формы</summary>
    public FormCommitHooks Commits { get; } = new();

    /// <summary>Живые редакторы полей (блочный, код) — одни на все зоны формы</summary>
    public FormLiveEditors LiveEditors { get; } = new();

    MetaValueStore? _store;

    IFormValueStore Store => _store ??= new MetaValueStore(MetaValues, MetaFields);

    /// <summary>Забрать значения всех тяжёлых редакторов в модель — вызывать перед сохранением формы</summary>
    public Task CommitAllAsync() => Commits.CommitAllAsync();
}
