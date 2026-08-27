using Mars.Contracts.MetaFields;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Реестр редакторов значений мета-полей: ключ (<c>Options.editor</c>) →
/// компонент + совместимые типы полей. Первая волна встроенная; расширение списка —
/// позже через фронт-плагины.
/// </summary>
public interface IMetaFieldEditorLocator
{
    /// <summary>Компонент редактора для поля; null — ключ отсутствует или несовместим с типом (дефолтный редактор)</summary>
    Type? GetEditorComponent(string? editorKey, MetaFieldType fieldType);

    /// <summary>Редакторы, совместимые с типом поля (для выбора в настройках поля)</summary>
    IReadOnlyCollection<(string Key, string Title)> EditorsFor(MetaFieldType fieldType);
}

public class MetaFieldEditorLocator : IMetaFieldEditorLocator
{
    static readonly Dictionary<string, (Type Component, MetaFieldType[] FieldTypes)> Registry = new()
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

    /// <summary>
    /// Динамическая регистрация редактора (админка, плагины) — до рендеринга.
    /// Так тяжёлые/модульные редакторы не тянут статических ссылок из общей
    /// фронт-библиотеки: ключ в <see cref="MetaFieldEditorCatalog"/> есть всегда,
    /// а компонент появляется только там, где его зарегистрировали.
    /// </summary>
    public static void Register(string editorKey, Type component, params MetaFieldType[] fieldTypes)
    {
        lock (RegistrationLock)
        {
            Registry[editorKey] = (component, fieldTypes);
        }
    }

    public Type? GetEditorComponent(string? editorKey, MetaFieldType fieldType)
    {
        if (string.IsNullOrEmpty(editorKey)) return null;
        if (!Registry.TryGetValue(editorKey, out var entry)) return null;

        return entry.FieldTypes.Contains(fieldType) ? entry.Component : null;
    }

    public IReadOnlyCollection<(string Key, string Title)> EditorsFor(MetaFieldType fieldType)
        => Registry.Where(kv => kv.Value.FieldTypes.Contains(fieldType))
                   .Select(kv => (kv.Key, MetaFieldEditorCatalog.All.First(a => a.Key == kv.Key).Title))
                   .ToList();
}
