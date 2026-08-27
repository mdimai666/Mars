using System.Text.Json.Nodes;
using Mars.Contracts.PostTypes;

namespace Mars.Contracts.MetaFields;

/// <summary>
/// Каталог мета-полей, требуемых фичами типа поста (общий для сервера и админки).
/// Поле, созданное фичей, помечается <c>Options.featureKey</c> и защищено от
/// удаления, пока фича включена и поле выбрано указателем фичи.
/// </summary>
public static class FeatureFieldsCatalog
{
    /// <summary>Маркер поля картинки типа (фича <see cref="PostTypeConstants.Features.PostImage"/>)</summary>
    public const string PostImage = "postImage";

    /// <summary>Ключ автосоздаваемого поля картинки</summary>
    public const string PostImageFieldKey = "image";

    /// <summary>Заголовок автосоздаваемого поля картинки</summary>
    public const string PostImageFieldTitle = "Изображение";

    /// <summary>Маркер поля контента типа (фича <see cref="PostTypeConstants.Features.Content"/>)</summary>
    public const string Content = "content";

    /// <summary>Ключ поля контента (фиксированный, переименование запрещено)</summary>
    public const string ContentFieldKey = "content";

    /// <summary>Заголовок автосоздаваемого поля контента</summary>
    public const string ContentFieldTitle = "Контент";

    /// <summary>Ключ опции маркера фичи в Options</summary>
    public static string FeatureKeyOption() => "featureKey";

    /// <summary>Маркер фичи поля из <c>Options.featureKey</c> (пусто = обычное поле)</summary>
    public static string GetFeatureKey(this JsonNode? options)
    {
        if (options is not JsonObject obj) return "";
        if (obj[FeatureKeyOption()] is JsonValue value && value.TryGetValue<string>(out var key)) return key;

        return "";
    }

    /// <summary>Поле создано фичей типа (маркер происхождения в Options)</summary>
    public static bool IsFeatureRequired(this JsonNode? options)
        => options.GetFeatureKey().Length > 0;

    /// <summary>Копия Options без маркера фичи (например, для клонов); пустой объект → null</summary>
    public static JsonNode? WithoutFeatureKey(this JsonNode? options)
    {
        if (options is not JsonObject obj) return options;

        var copy = (JsonObject)obj.DeepClone();
        copy.Remove(FeatureKeyOption());
        return copy.Count == 0 ? null : copy;
    }

    /// <summary>Фича → маркер требуемого ей поля (null = фича не требует поля)</summary>
    public static string? GetFeatureKeyFor(string feature)
        => feature == PostTypeConstants.Features.PostImage ? PostImage
        : feature == PostTypeConstants.Features.Content ? Content
        : null;

    /// <summary>Маркер поля → имя фичи (для подписей; пусто, если маркер неизвестен)</summary>
    public static string GetFeatureName(string featureKey)
        => featureKey == PostImage ? PostTypeConstants.Features.PostImage
        : featureKey == Content ? PostTypeConstants.Features.Content
        : "";
}
