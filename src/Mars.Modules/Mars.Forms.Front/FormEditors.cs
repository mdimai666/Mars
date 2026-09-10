using Mars.Forms.Contracts;
using Mars.Forms.Front.Editors;

namespace Mars.Forms.Front;

/// <summary>
/// Реестр редакторов значений формы: ключ редактора → компонент + совместимые типы полей.
/// Паттерн тот же, что у редакторов мета-полей: встроенные примитивы здесь, тяжёлые и доменные
/// (WYSIWYG, пикеры связей, медиа) регистрируются потребителями через <see cref="Register"/>.
/// Компонент редактора принимает один параметр — <see cref="FormFieldBinding"/>.
/// </summary>
public interface IFormEditorLocator
{
    /// <summary>Компонент редактора; null — ключ неизвестен или несовместим с типом поля</summary>
    Type? GetEditorComponent(string? editorKey, FormFieldType fieldType, bool multiple);

    /// <summary>Встроенный редактор по типу поля (когда явный ключ не задан); null — встроенного нет</summary>
    Type? GetDefaultEditor(FormFieldType fieldType, bool multiple);

    /// <summary>Редакторы, совместимые с полем (для выбора в дизайнере формы)</summary>
    IReadOnlyCollection<(string Key, string Title)> EditorsFor(FormFieldType fieldType, bool multiple);
}

/// <summary>Реестр компонентов-контейнеров: вид контейнера → компонент (провайдеры добавляют свои)</summary>
public interface IFormContainerLocator
{
    Type? GetContainerComponent(string kind);

    IReadOnlyCollection<string> Kinds { get; }
}

public class FormEditorLocator : IFormEditorLocator
{
    static readonly Dictionary<string, (Type Component, FormFieldType[] FieldTypes, bool Multiple)> Registry = new(StringComparer.Ordinal)
    {
        [FormEditorCatalog.Text] = (typeof(FormStringEditor), [FormFieldType.String], false),
        [FormEditorCatalog.Multiline] = (typeof(FormTextEditor), [FormFieldType.String, FormFieldType.Text], false),
        [FormEditorCatalog.Number] = (typeof(FormNumberEditor),
            [FormFieldType.Int, FormFieldType.Long, FormFieldType.Float, FormFieldType.Decimal], false),
        [FormEditorCatalog.Bool] = (typeof(FormBoolEditor), [FormFieldType.Bool], false),
        [FormEditorCatalog.Date] = (typeof(FormDateEditor), [FormFieldType.DateTime], false),
        [FormEditorCatalog.Select] = (typeof(FormSelectEditor), [FormFieldType.Select, FormFieldType.SelectMany], false),
        [FormEditorCatalog.Choices] = (typeof(FormChoicesEditor), [FormFieldType.SelectMany], false),
        [FormEditorCatalog.List] = (typeof(FormListEditor),
            [FormFieldType.String, FormFieldType.Int, FormFieldType.Long, FormFieldType.Float,
             FormFieldType.Decimal, FormFieldType.DateTime, FormFieldType.Select], true),
    };

    static readonly object RegistrationLock = new();

    /// <summary>Встроенный редактор типа, когда явный ключ редактора не задан</summary>
    static readonly Dictionary<FormFieldType, string> DefaultKeys = new()
    {
        [FormFieldType.String] = FormEditorCatalog.Text,
        [FormFieldType.Text] = FormEditorCatalog.Multiline,
        [FormFieldType.Bool] = FormEditorCatalog.Bool,
        [FormFieldType.Int] = FormEditorCatalog.Number,
        [FormFieldType.Long] = FormEditorCatalog.Number,
        [FormFieldType.Float] = FormEditorCatalog.Number,
        [FormFieldType.Decimal] = FormEditorCatalog.Number,
        [FormFieldType.DateTime] = FormEditorCatalog.Date,
        [FormFieldType.Select] = FormEditorCatalog.Select,
    };

    /// <summary>
    /// Регистрация редактора (админка, модули, плагины) — до рендеринга.
    /// Тяжёлые и доменные редакторы не тянут статических ссылок из общей библиотеки:
    /// ключ в манифесте провайдера есть всегда, компонент появляется там, где его зарегистрировали.
    /// </summary>
    public static void Register(string editorKey, Type component, bool multiple, params FormFieldType[] fieldTypes)
    {
        lock (RegistrationLock)
        {
            Registry[editorKey] = (component, fieldTypes, multiple);
        }
    }

    public Type? GetEditorComponent(string? editorKey, FormFieldType fieldType, bool multiple)
    {
        if (string.IsNullOrEmpty(editorKey)) return null;

        lock (RegistrationLock)
        {
            if (!Registry.TryGetValue(editorKey, out var entry)) return null;
            return entry.Multiple == multiple && entry.FieldTypes.Contains(fieldType) ? entry.Component : null;
        }
    }

    public Type? GetDefaultEditor(FormFieldType fieldType, bool multiple)
    {
        // множественный выбор — чекбоксы вариантов поля, а не общий список значений
        if (fieldType == FormFieldType.SelectMany && !multiple)
            return GetEditorComponent(FormEditorCatalog.Choices, fieldType, false);

        // SelectMany — всегда список значений, независимо от флага кратности
        var isList = multiple || fieldType == FormFieldType.SelectMany;
        var elementType = fieldType == FormFieldType.SelectMany ? FormFieldType.Select : fieldType;

        return isList
            ? GetEditorComponent(FormEditorCatalog.List, elementType, true)
            : DefaultKeys.TryGetValue(fieldType, out var key)
                ? GetEditorComponent(key, fieldType, false)
                : null;
    }

    public IReadOnlyCollection<(string Key, string Title)> EditorsFor(FormFieldType fieldType, bool multiple)
    {
        lock (RegistrationLock)
        {
            return Registry.Where(kv => kv.Value.Multiple == multiple && kv.Value.FieldTypes.Contains(fieldType))
                           .Select(kv => (kv.Key, Title(kv.Key)))
                           .ToList();
        }
    }

    static string Title(string key)
        => FormEditorCatalog.All.FirstOrDefault(entry => entry.Key == key).Title ?? key;
}

public class FormContainerLocator : IFormContainerLocator
{
    static readonly Dictionary<string, Type> Registry = new(StringComparer.Ordinal)
    {
        [FormItemKinds.Section] = typeof(FormSectionBlock),
    };

    static readonly object RegistrationLock = new();

    /// <summary>Регистрация своего вида контейнера (провайдер/плагин)</summary>
    public static void Register(string kind, Type component)
    {
        lock (RegistrationLock) Registry[kind] = component;
    }

    public Type? GetContainerComponent(string kind)
    {
        lock (RegistrationLock) return Registry.TryGetValue(kind, out var component) ? component : null;
    }

    public IReadOnlyCollection<string> Kinds
    {
        get
        {
            lock (RegistrationLock) return Registry.Keys.ToList();
        }
    }
}
