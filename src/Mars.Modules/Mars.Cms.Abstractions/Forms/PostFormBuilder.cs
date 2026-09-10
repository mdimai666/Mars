using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Mappings.Forms;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Abstractions;
using Mars.Forms.Contracts;
using static Mars.Cms.Contracts.PostTypes.SystemFieldsCatalog;

namespace Mars.Cms.Abstractions.Forms;

/// <summary>
/// Сборка определения формы поста: системные слоты (<see cref="SystemFieldsCatalog"/>) и метаполя типа
/// складываются в дерево по умолчанию (порядок исторической разметки формы), затем нормализуются
/// сохранённой раскладкой из <c>post_types.Options["form"]</c>. Дескрипторы всегда свежие —
/// раскладка хранит только порядок, зоны и настройки.
/// </summary>
public static class PostFormBuilder
{
    public const string OwnerModelPrefix = "post";

    /// <summary>Ключ регистрации провайдера: один провайдер на все типы постов</summary>
    public const string OwnerModelWildcard = OwnerModelPrefix + ".*";

    public static string OwnerModel(string typeName) => $"{OwnerModelPrefix}.{typeName}";

    public static string PostTypeName(string ownerModel)
        => ownerModel.StartsWith(OwnerModelPrefix + ".", StringComparison.Ordinal)
            ? ownerModel[(OwnerModelPrefix.Length + 1)..]
            : ownerModel;

    public static FormDefinition Build(PostTypeDetail postType, IFormDefinitionNormalizer normalizer, bool client = false)
        => new()
        {
            OwnerModel = OwnerModel(postType.TypeName),
            Zones = SystemFieldsCatalog.Zones.All,
            Items = normalizer.Normalize(postType.Form?.Items, DefaultItems(postType, client)),
        };

    /// <summary>Дерево по умолчанию: то, что форма показывала до перехода на раскладку</summary>
    public static IReadOnlyCollection<FormItem> DefaultItems(PostTypeDetail postType, bool client = false)
    {
        var features = postType.EnabledFeatures;
        var items = new List<FormItem>();

        AddSlot(Title);
        AddSlot(Slug);

        var content = postType.ContentField();
        if (content is not null) items.Add(MetaItem(content));

        AddSlot(Excerpt);

        foreach (var field in postType.MetaFields
                     .Where(f => !f.Disabled)
                     .Where(f => f.Type != MetaFieldType.Query)
                     .Where(f => !client || !f.Hidden)
                     // контент уже размещён выше — в общем потоке метаполей он не участвует
                     .Where(f => f.Options.GetFeatureKey() != FeatureFieldsCatalog.Content)
                     .OrderBy(f => f.Order))
            items.Add(MetaItem(field));

        AddSlot(CreatedAt);
        AddSlot(ModifiedAt);
        AddSlot(Status);
        AddSlot(Lang);
        AddSlot(Author);

        AddSlot(Categories);
        AddSlot(Tags);

        return items;

        void AddSlot(string key)
        {
            var slot = Find(key);
            if (slot is null) return;
            if (slot.Feature is not null && !features.Contains(slot.Feature)) return;

            items.Add(SlotItem(slot, postType));
        }
    }

    static FormItem SlotItem(SystemFieldSlot slot, PostTypeDetail postType)
    {
        // параметры слота (правила, редактор) — из настроек типа; раскладка формы их больше не хранит
        var settings = postType.SystemFields?.FirstOrDefault(s => s.Key == slot.Key);

        var descriptor = new FormFieldDescriptor
        {
            Key = slot.Key,
            Title = slot.TitleKey,
            TitleKey = slot.TitleKey,
            Type = slot.Type,
            Required = IsRequired(slot),
            ReadOnly = IsReadOnly(slot, postType.EnabledFeatures),
            Multiple = slot.Multiple,
            Editor = settings?.Editor ?? slot.Editor,
            ModelName = slot.ModelName,
            Choices = slot.Key == Status
                ? postType.PostStatusList.Select(s => new FormChoiceOption { Key = s.Slug, Title = s.Title }).ToList()
                : [],
            Rules = settings?.Rules ?? [],
            SettingsOnForm = true,
        };

        return new FormItem { Key = slot.Key, Zone = slot.Zone, Field = descriptor };
    }

    static FormItem MetaItem(MetaFieldDto field) => new()
    {
        Key = field.Key,
        Zone = Zones.Main,
        Field = field.ToFormFieldDescriptor(),
    };
}
