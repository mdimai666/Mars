using Mars.Cms.Contracts.MetaFields;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Реестр редакторов значений мета-полей: ключ (<see cref="MetaFieldEditorCatalog"/>) → компонент +
/// совместимые типы поля. Контракт параметров у этих компонентов свой — <c>Value</c>/<c>ValueChanged</c>
/// (<see cref="MetaValueEditModel"/>), поэтому реестр отделён от общего реестра редакторов формы
/// (<c>IFormEditorLocator</c>, контракт <c>Binding</c>): ключи каталогов пересекаются
/// (например <c>core.input.date</c>), а компоненты невзаимозаменяемы.
/// Общий рендерер формы приходит сюда через зарегистрированную обёртку <c>MetaValueRowEditor</c>.
/// Тяжёлые и модульные редакторы (блочный Editor.js) регистрирует потребитель: их компоненты живут
/// вне общей библиотеки.
/// </summary>
public static class MetaFieldEditors
{
    static readonly Dictionary<string, (Type Component, IReadOnlyCollection<MetaFieldType> Types)> Registry = new()
    {
        [MetaFieldEditorCatalog.Color] = (typeof(Editors.MetaValueColorEditor), [MetaFieldType.String]),
        [MetaFieldEditorCatalog.Url] = (typeof(Editors.MetaValueUrlEditor), [MetaFieldType.String]),
        [MetaFieldEditorCatalog.Email] = (typeof(Editors.MetaValueEmailEditor), [MetaFieldType.String]),
        [MetaFieldEditorCatalog.Date] = (typeof(Editors.MetaValueDateEditor), [MetaFieldType.DateTime]),
        [MetaFieldEditorCatalog.Time] = (typeof(Editors.MetaValueTimeEditor), [MetaFieldType.DateTime]),
        [MetaFieldEditorCatalog.DateTime] = (typeof(Editors.MetaValueDateTimeEditor), [MetaFieldType.DateTime]),
        [MetaFieldEditorCatalog.Wysiwyg] = (typeof(Editors.MetaValueWysiwygEditor), [MetaFieldType.String, MetaFieldType.Text]),
        [MetaFieldEditorCatalog.Code] = (typeof(Editors.MetaValueCodeEditor), [MetaFieldType.String, MetaFieldType.Text]),
    };

    static readonly object RegistrationLock = new();

    /// <summary>Регистрация редактора (модуль, плагин, админка) — до рендеринга</summary>
    public static void Register(string editorKey, Type component, params MetaFieldType[] fieldTypes)
    {
        lock (RegistrationLock)
        {
            Registry[editorKey] = (component, fieldTypes);
        }
    }

    /// <summary>
    /// Компонент редактора по ключу. Ключ пустой или несовместим с типом поля — null
    /// (рендерер значения берёт дефолтный редактор типа).
    /// </summary>
    public static Type? GetEditorComponent(string? editorKey, MetaFieldType fieldType)
    {
        if (string.IsNullOrEmpty(editorKey)) return null;

        lock (RegistrationLock)
        {
            if (!Registry.TryGetValue(editorKey, out var entry)) return null;
            return entry.Types.Contains(fieldType) ? entry.Component : null;
        }
    }

    /// <summary>Редакторы, доступные для типа поля — для UI выбора редактора в настройках поля</summary>
    public static IReadOnlyCollection<(string Key, string Title)> EditorsFor(MetaFieldType fieldType)
    {
        lock (RegistrationLock)
        {
            return Registry.Where(entry => entry.Value.Types.Contains(fieldType))
                           .Select(entry => (entry.Key, MetaFieldEditorCatalog.All
                                                             .FirstOrDefault(x => x.Key == entry.Key).Title ?? entry.Key))
                           .ToList();
        }
    }
}
