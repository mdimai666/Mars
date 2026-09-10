using Mars.Forms.Contracts;
using Mars.Forms.Front.Editors;

namespace Mars.Forms.Front;

/// <summary>
/// Реестр редакторов значений формы: ключ редактора → компонент + совместимые типы полей.
/// Встроенные примитивы вшиты, тяжёлые и доменные (WYSIWYG, код, блочный, пикеры связей и медиа)
/// приходят регистрациями DI — <c>AddFormEditor</c> в админке, модулях и плагинах.
/// Компонент редактора принимает один параметр — <see cref="FormFieldBinding"/>.
/// </summary>
public interface IFormEditorLocator
{
    /// <summary>Компонент редактора; null — ключ неизвестен или несовместим с типом поля</summary>
    Type? GetEditorComponent(string? editorKey, FormFieldType fieldType, bool multiple);

    /// <summary>Встроенный редактор по типу поля (когда явный ключ не задан); null — встроенного нет</summary>
    Type? GetDefaultEditor(FormFieldType fieldType, bool multiple);

    /// <summary>Редакторы, совместимые с полем и предлагаемые в UI выбора</summary>
    IReadOnlyCollection<(string Key, string Title)> EditorsFor(FormFieldType fieldType, bool multiple);
}

/// <summary>
/// Регистрация редактора значения: ключ → компонент + совместимые типы полей.
/// Название делает редактор предлагаемым в UI выбора; безымянные регистрации (обёртки
/// провайдеров, встроенные дефолты) доступны только явным ключом дескриптора.
/// Один и тот же ключ, зарегистрированный дважды, перекрывается последним.
/// </summary>
public sealed record FormEditorRegistration(string Key, Type Component, bool Multiple, string? Title,
                                             IReadOnlyCollection<FormFieldType> FieldTypes);

public sealed class FormEditorLocator : IFormEditorLocator
{
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

    /// <summary>Встроенные редакторы общего слоя; регистрация потребителя с тем же ключом их перекрывает</summary>
    static readonly IReadOnlyList<FormEditorRegistration> BuiltIn =
    [
        new(FormEditorCatalog.Text, typeof(FormStringEditor), false, null, [FormFieldType.String]),
        new(FormEditorCatalog.Multiline, typeof(FormTextEditor), false, null, [FormFieldType.String, FormFieldType.Text]),
        new(FormEditorCatalog.Number, typeof(FormNumberEditor), false, null,
            [FormFieldType.Int, FormFieldType.Long, FormFieldType.Float, FormFieldType.Decimal]),
        new(FormEditorCatalog.Bool, typeof(FormBoolEditor), false, null, [FormFieldType.Bool]),
        new(FormEditorCatalog.Date, typeof(FormDateEditor), false, null, [FormFieldType.DateTime]),
        new(FormEditorCatalog.Select, typeof(FormSelectEditor), false, null,
            [FormFieldType.Select, FormFieldType.SelectMany]),
        new(FormEditorCatalog.Choices, typeof(FormChoicesEditor), false, null, [FormFieldType.SelectMany]),
        new(FormEditorCatalog.List, typeof(FormListEditor), true, null,
            [FormFieldType.String, FormFieldType.Int, FormFieldType.Long, FormFieldType.Float,
             FormFieldType.Decimal, FormFieldType.DateTime, FormFieldType.Select]),
    ];

    readonly Dictionary<string, FormEditorRegistration> _registry;

    public FormEditorLocator(IEnumerable<FormEditorRegistration>? registrations = null)
    {
        _registry = new Dictionary<string, FormEditorRegistration>(StringComparer.Ordinal);

        // встроенные, затем регистрации потребителей: их ключ перекрывает встроенный
        foreach (var builtIn in BuiltIn) _registry[builtIn.Key] = builtIn;
        foreach (var registration in registrations ?? []) _registry[registration.Key] = registration;
    }

    public Type? GetEditorComponent(string? editorKey, FormFieldType fieldType, bool multiple)
        => !string.IsNullOrEmpty(editorKey)
           && _registry.TryGetValue(editorKey, out var entry)
           && entry.Multiple == multiple
           && entry.FieldTypes.Contains(fieldType)
            ? entry.Component
            : null;

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
        => _registry.Values
                    .Where(entry => !string.IsNullOrEmpty(entry.Title)
                                    && entry.Multiple == multiple
                                    && entry.FieldTypes.Contains(fieldType))
                    .Select(entry => (entry.Key, entry.Title!))
                    .ToList();
}
