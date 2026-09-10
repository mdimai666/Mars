using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Mappings.Forms;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;

namespace Mars.Cms.Abstractions.Forms;

/// <summary>
/// Сборка определения формы владельца, у которого нет системных слотов: только метаполя типа,
/// плоским списком в одну зону (пользователи, категории постов). Сохранённой раскладки у таких
/// владельцев пока нет — порядок задаёт <c>MetaField.Order</c>, как в прежнем рендере списка
/// метаполей, поэтому нормализатор не нужен.
/// </summary>
public static class MetaFieldsFormBuilder
{
    public const string UserPrefix = "user";

    public const string PostCategoryPrefix = "postcategory";

    public static string UserOwnerModel(string typeName) => $"{UserPrefix}.{typeName}";

    public static string PostCategoryOwnerModel(string typeName) => $"{PostCategoryPrefix}.{typeName}";

    public static FormDefinition Build(string ownerModel, string title, IReadOnlyCollection<MetaFieldDto> fields, bool client = false)
        => new()
        {
            OwnerModel = ownerModel,
            Items = DefaultItems(fields, client),
            Manifest = Manifest(ownerModel, title),
        };

    /// <summary>Дерево по умолчанию: тот же состав и порядок, что рендерил список метаполей</summary>
    public static IReadOnlyCollection<FormItem> DefaultItems(IReadOnlyCollection<MetaFieldDto> fields, bool client = false)
        => fields.Where(f => !f.Disabled)
                 .Where(f => f.Type != MetaFieldType.Query)
                 .Where(f => !client || !f.Hidden)
                 .OrderBy(f => f.Order)
                 .Select(f => new FormItem { Key = f.Key, Zone = Zone, Field = f.ToFormFieldDescriptor() })
                 .ToList();

    public static FormProviderManifest Manifest(string ownerModel, string title) => new()
    {
        OwnerModel = ownerModel,
        Title = title,
        Zones = [new FormZoneDescriptor { Key = Zone, Title = "Основное" }],
        ContainerKinds = FormItemKinds.All,
        RuleTypes = FormRuleCatalog.All,
        EditorKeys = [],
        Capabilities = new FormProviderCapabilities
        {
            // поля заданы типом владельца, а значения ходят его типизированным транспортом
            CanAddFields = false,
            CanReadValues = false,
            CanSubmit = false,
        },
    };

    /// <summary>Единственная зона формы — общая для платформы зона основного контента</summary>
    public const string Zone = SystemFieldsCatalog.Zones.Main;
}
