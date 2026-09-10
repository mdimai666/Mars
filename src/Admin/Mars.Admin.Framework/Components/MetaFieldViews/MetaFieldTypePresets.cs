using Mars.Cms.Contracts.MetaFields;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Человеческие пресеты типов полей для пикера в админке: пресет = тип + редактор.
/// После применения пресета поле редактируется как обычный набор настроек.
/// Реализация фронтовая, без миграций.
/// </summary>
public static class MetaFieldTypePresets
{
    public record Preset(string Key, string Title, MetaFieldType Type, string Group, string? Editor = null, string? Kind = null, string? CodeLang = null, bool IsMultiple = false);

    const string PresetPrefix = "preset:";
    const string TypePrefix = "type:";

    /// <summary>Группы пикера (вложенные меню)</summary>
    public const string GroupText = "Текстовые";
    public const string GroupNumber = "Числовые";
    public const string GroupChoice = "Выбор";
    public const string GroupDate = "Даты";
    public const string GroupRelation = "Связь";
    public const string GroupRaw = "Raw";

    public static IReadOnlyCollection<Preset> All { get; } =
    [
        new("text", "Текст", MetaFieldType.String, GroupText),
        new("longtext", "Длинный текст", MetaFieldType.Text, GroupText),
        new("color", "Цвет", MetaFieldType.String, GroupText, MetaFieldEditorCatalog.Color),
        new("url", "Ссылка", MetaFieldType.String, GroupText, MetaFieldEditorCatalog.Url),
        new("email", "Email", MetaFieldType.String, GroupText, MetaFieldEditorCatalog.Email),
        new("wysiwyg", "Текст (WYSIWYG)", MetaFieldType.Text, GroupText, MetaFieldEditorCatalog.Wysiwyg),
        new("blockeditor", "Текст (Editor.js)", MetaFieldType.Text, GroupText, MetaFieldEditorCatalog.BlockEditor),
        new("code", "Код", MetaFieldType.Text, GroupText, MetaFieldEditorCatalog.Code, CodeLang: MetaFieldEditorCatalog.DefaultCodeLang),

        new("number", "Число", MetaFieldType.Int, GroupNumber),
        new("longnumber", "Большое число", MetaFieldType.Long, GroupNumber),
        new("decimalnumber", "Дробное", MetaFieldType.Decimal, GroupNumber),

        new("bool", "Да/Нет", MetaFieldType.Bool, GroupChoice),
        new("select", "Выбор из списка", MetaFieldType.Select, GroupChoice),
        new("selectmany", "Множественный выбор", MetaFieldType.SelectMany, GroupChoice),

        new("datetime", "Дата и время", MetaFieldType.DateTime, GroupDate, MetaFieldEditorCatalog.DateTime),
        new("date", "Дата", MetaFieldType.DateTime, GroupDate, MetaFieldEditorCatalog.Date),
        new("time", "Время", MetaFieldType.DateTime, GroupDate, MetaFieldEditorCatalog.Time),

        new("relation", "Связь", MetaFieldType.Relation, GroupRelation),
        new("relationmulti", "Несколько связей", MetaFieldType.Relation, GroupRelation, IsMultiple: true),
        new("list", "Список объектов", MetaFieldType.Relation, GroupRelation, null, MetaFieldKindCatalog.List, IsMultiple: true),
        new("file", "Файл", MetaFieldType.File, GroupRelation),
        new("image", "Изображение", MetaFieldType.Image, GroupRelation),
        new("imageset", "Набор изображений", MetaFieldType.Image, GroupRelation, IsMultiple: true),
    ];

    public static string OptionKey(Preset preset) => PresetPrefix + preset.Key;
    public static string OptionKey(MetaFieldType type) => TypePrefix + type;

    /// <summary>Пункт пикера типа поля (для группового выпадающего списка)</summary>
    public record PickerItem(string Group, string OptionKey, string Label);

    /// <summary>Все пункты пикера: пресеты по группам + сырые типы в «Raw»</summary>
    public static IReadOnlyCollection<PickerItem> PickerItems { get; } =
    [
        .. All.Select(p => new PickerItem(p.Group, OptionKey(p), MetaFieldEditModel.TypeIcons[p.Type] + " " + p.Title)),
        .. Enum.GetValues<MetaFieldType>().Select(t => new PickerItem(GroupRaw, OptionKey(t), MetaFieldEditModel.TypeIcons[t] + " " + MetaFieldEditModel.TypeList[t])),
    ];

    public static Preset? FindPreset(string optionKey)
        => optionKey.StartsWith(PresetPrefix) ? All.FirstOrDefault(p => OptionKey(p) == optionKey) : null;

    public static MetaFieldType? FindType(string optionKey)
        => optionKey.StartsWith(TypePrefix)
           && Enum.TryParse<MetaFieldType>(optionKey[TypePrefix.Length..], out var type)
            ? type
            : null;
}
