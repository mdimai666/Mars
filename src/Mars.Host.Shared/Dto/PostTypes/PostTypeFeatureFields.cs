using System.Text.Json.Nodes;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.PostTypes;

/// <summary>
/// Тулза применения фич типа поста к составу мета-полей. Пайплайн сохранения
/// типа её не использует (согласованность — на клиенте и валидаторе); доступна
/// для сервисного кода, тестов и миграций данных.
/// </summary>
public static class PostTypeFeatureFields
{
    /// <summary>
    /// Применение фичи «Картинка поста» к списку полей.
    /// <paramref name="enable"/> = true: указатель <paramref name="key"/> должен ссылаться
    /// на поле типа Изображение; если он пуст или поле не найдено — поле создаётся
    /// (маркер <see cref="FeatureFieldsCatalog.PostImage"/> в Options).
    /// <paramref name="enable"/> = false: указатель очищается, поля не трогаются.
    /// </summary>
    public static (IReadOnlyCollection<MetaFieldDto> Fields, string? ImageFieldKey) ApplyFeaturePostImage(
        IReadOnlyCollection<MetaFieldDto> fields, bool enable, string? key = null)
    {
        if (!enable) return (fields, null);

        var list = fields.ToList();

        var target = string.IsNullOrEmpty(key)
            ? null
            : list.FirstOrDefault(f => f.Key == key && f.Type == MetaFieldType.Image);
        if (target is not null) return (list, target.Key);

        var created = CreatePostImageField(list);
        list.Add(created);
        return (list, created.Key);
    }

    /// <summary>
    /// Применение фичи «Контент» к списку полей.
    /// <paramref name="enable"/> = true: поля с ключом <see cref="FeatureFieldsCatalog.ContentFieldKey"/>
    /// нет — оно создаётся (тип Текст, маркер <see cref="FeatureFieldsCatalog.Content"/> в Options,
    /// редактор — блочный). <paramref name="enable"/> = false: поля не трогаются.
    /// </summary>
    public static IReadOnlyCollection<MetaFieldDto> ApplyFeatureContent(
        IReadOnlyCollection<MetaFieldDto> fields, bool enable)
    {
        if (!enable) return fields;

        var list = fields.ToList();
        if (list.Any(f => f.Key == FeatureFieldsCatalog.ContentFieldKey)) return list;

        list.Add(CreateContentField(list));
        return list;
    }

    static MetaFieldDto CreatePostImageField(IReadOnlyCollection<MetaFieldDto> currentFields)
    {
        var takenKeys = new HashSet<string>(currentFields.Select(f => f.Key), StringComparer.Ordinal);
        var key = FeatureFieldsCatalog.PostImageFieldKey;
        var suffix = 2;
        while (!takenKeys.Add(key))
        {
            key = $"{FeatureFieldsCatalog.PostImageFieldKey}_{suffix}";
            suffix++;
        }

        return new MetaFieldDto
        {
            Id = Guid.NewGuid(),
            Title = FeatureFieldsCatalog.PostImageFieldTitle,
            Key = key,
            Type = MetaFieldType.Image,
            MaxValue = null,
            MinValue = null,
            Description = "",
            IsNullable = true,
            IsMultiple = false,
            Default = null,
            Options = new JsonObject
            {
                [FeatureFieldsCatalog.FeatureKeyOption()] = FeatureFieldsCatalog.PostImage,
            },
            Order = currentFields.Count == 0 ? 0 : currentFields.Max(f => f.Order) + 1,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = [],
            ModelName = null,
        };
    }

    static MetaFieldDto CreateContentField(IReadOnlyCollection<MetaFieldDto> currentFields)
    {
        return new MetaFieldDto
        {
            Id = Guid.NewGuid(),
            Title = FeatureFieldsCatalog.ContentFieldTitle,
            Key = FeatureFieldsCatalog.ContentFieldKey,
            Type = MetaFieldType.Text,
            MaxValue = null,
            MinValue = null,
            Description = "",
            IsNullable = true,
            IsMultiple = false,
            Default = null,
            Options = new JsonObject
            {
                [FeatureFieldsCatalog.FeatureKeyOption()] = FeatureFieldsCatalog.Content,
                [MetaFieldEditorCatalog.EditorOption()] = MetaFieldEditorCatalog.BlockEditor,
            },
            Order = currentFields.Count == 0 ? 0 : currentFields.Max(f => f.Order) + 1,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = [],
            ModelName = null,
        };
    }
}
