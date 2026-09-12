using Mars.Forms.Contracts;
using Mars.Forms.Front.Editors;

namespace Mars.Forms.Front;

/// <summary>
/// Реестр редакторов значений формы: ключ редактора → компонент + совместимые типы полей.
/// Встроенные примитивы вшиты, тяжёлые и доменные (WYSIWYG, код, блочный, пикеры связей и медиа)
/// регистрируют админка, модули и плагины через <see cref="Register"/> — реестр открыт откуда
/// угодно, а записи собираются в момент запроса (см. <see cref="FormEditorLocator"/>).
/// Компонент редактора принимает один параметр — <see cref="FormFieldBinding"/>.
/// </summary>
public interface IFormEditorLocator
{
    /// <summary>
    /// Регистрация редактора значения. Название делает редактор предлагаемым в UI выбора;
    /// безымянные регистрации (обёртки провайдеров, встроенные дефолты) доступны только явным
    /// ключом дескриптора. Один и тот же ключ, зарегистрированный дважды, перекрывается последним.
    /// </summary>
    void Register(string editorKey, Type component, bool multiple, string? title, params FormFieldType[] fieldTypes);

    /// <summary>Компонент редактора; null — ключ неизвестен или несовместим с типом поля</summary>
    Type? GetEditorComponent(string? editorKey, FormFieldType fieldType, bool multiple);

    /// <summary>Встроенный редактор по типу поля (когда явный ключ не задан); null — встроенного нет</summary>
    Type? GetDefaultEditor(FormFieldType fieldType, bool multiple);

    /// <summary>Редакторы, совместимые с полем и предлагаемые в UI выбора</summary>
    IReadOnlyCollection<(string Key, string Title)> EditorsFor(FormFieldType fieldType, bool multiple);
}

/// <summary>Запись реестра редакторов: ключ → компонент + совместимые типы полей</summary>
public sealed record FormEditorRegistration(string Key, Type Component, bool Multiple, string? Title,
                                             IReadOnlyCollection<FormFieldType> FieldTypes);

/// <summary>
/// Экземпляр реестра редакторов (регистрируется синглтоном в DI): зарегистрировать редактор можно
/// откуда угодно и когда угодно — записи складываются в список, а словарь по ключу собирается
/// в момент запроса и подменяется атомарно (аналогично локатору типов нод).
/// </summary>
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

    readonly object _lock = new();
    readonly List<FormEditorRegistration> _registrations = [];
    volatile Dictionary<string, FormEditorRegistration>? _registry;

    public FormEditorLocator() => _registrations.AddRange(BuiltIn);

    public void Register(string editorKey, Type component, bool multiple, string? title, params FormFieldType[] fieldTypes)
    {
        lock (_lock)
        {
            _registrations.Add(new FormEditorRegistration(editorKey, component, multiple, title, fieldTypes));
            _registry = null;
        }
    }

    public Type? GetEditorComponent(string? editorKey, FormFieldType fieldType, bool multiple)
        => !string.IsNullOrEmpty(editorKey)
           && Registry.TryGetValue(editorKey, out var entry)
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
        => Registry.Values
                   .Where(entry => !string.IsNullOrEmpty(entry.Title)
                                   && entry.Multiple == multiple
                                   && entry.FieldTypes.Contains(fieldType))
                   .Select(entry => (entry.Key, entry.Title!))
                   .ToList();

    /// <summary>Словарь по ключу: собирается в новый экземпляр и подменяется атомарно, чтобы
    /// читатели из других потоков не увидели полусобранный или устаревший</summary>
    Dictionary<string, FormEditorRegistration> Registry
    {
        get
        {
            var registry = _registry;
            if (registry is not null) return registry;

            lock (_lock)
            {
                return _registry ??= _registrations
                    .GroupBy(registration => registration.Key, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
            }
        }
    }
}
