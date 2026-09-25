using System.Text.Json.Nodes;
using Mars.Forms.Contracts;

namespace Mars.Cms.Contracts.PostTypes;

/// <summary>
/// Каталог общих настроек типа поста (jsonb-мешок <c>PostTypeEntity.Options</c>).
/// Ключ хранится в Options типа; чтение — типизированные ридеры-расширения
/// (как у <c>MetaFieldKindCatalog</c>/<c>FormEditorCatalog</c>).
/// Ключи добавляются по мере появления фич, которые их используют.
/// Пер-постовые вещи — мета-поля, не опции типа.
/// </summary>
public static class PostTypeOptionsCatalog
{
    /// <summary>Раскладка формы редактирования типа (<see cref="FormLayoutSettings"/>)</summary>
    public const string Form = "form";

    /// <summary>Параметры системных полей типа (<see cref="FormFieldSettings"/>): правила и редактор слота</summary>
    public const string SystemFields = "systemFields";

    /// <summary>Раскладка формы из Options типа; отсутствует или битая — null (действует раскладка по умолчанию)</summary>
    public static FormLayoutSettings? GetFormLayout(this JsonNode? options)
        => options is JsonObject obj ? FormLayoutJson.Parse(obj[Form]) : null;

    /// <summary>Сохранённые параметры системных полей; отсутствуют или битые — null</summary>
    public static IReadOnlyCollection<FormFieldSettings>? GetSystemFields(this JsonNode? options)
        => options is JsonObject obj ? FormFieldSettingsJson.Parse(obj[SystemFields]) : null;

    /// <summary>Копия Options с заменённой раскладкой формы; null-раскладка убирает ключ, пустой мешок → null</summary>
    public static JsonNode? WithFormLayout(this JsonNode? options, FormLayoutSettings? layout)
    {
        var copy = options is JsonObject obj ? (JsonObject)obj.DeepClone() : new JsonObject();
        var node = layout.ToJsonNode();

        if (node is null) copy.Remove(Form);
        else copy[Form] = node;

        return copy.Count == 0 ? null : copy;
    }

    /// <summary>Копия Options с заменёнными параметрами системных полей; null/пустой набор убирает ключ</summary>
    public static JsonNode? WithSystemFields(this JsonNode? options, IReadOnlyCollection<FormFieldSettings>? settings)
    {
        var copy = options is JsonObject obj ? (JsonObject)obj.DeepClone() : new JsonObject();
        var node = settings.ToJsonNode();

        if (node is null) copy.Remove(SystemFields);
        else copy[SystemFields] = node;

        return copy.Count == 0 ? null : copy;
    }
}
