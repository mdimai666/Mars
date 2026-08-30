using System.Text.Json.Nodes;
using Mars.Cms.Contracts.MetaFields;
using Mars.Core.Interfaces;

namespace Mars.Cms.Abstractions.Dto.MetaFields;

public record MetaFieldDto : IHasId
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Key { get; init; }
    public required MetaFieldType Type { get; init; }

    public required decimal? MaxValue { get; init; }
    public required decimal? MinValue { get; init; }
    public required string Description { get; init; }
    public required bool IsNullable { get; init; }
    public required bool IsMultiple { get; init; }
    public required MetaFieldDefaultValue? Default { get; init; }
    public required JsonNode? Options { get; init; }
    public required int Order { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required bool Hidden { get; init; }
    public required bool Disabled { get; init; }

    public required IReadOnlyCollection<MetaFieldVariantDto>? Variants { get; init; }
    public required string? ModelName { get; init; }

    public bool IsTypeRelation => Type is MetaFieldType.Relation or MetaFieldType.File or MetaFieldType.Image;

    /// <summary>
    /// Key варианта из дефолтного значения (для Select)
    /// </summary>
    public string? GetDefaultVariantKey()
        => Default?.VariantId is Guid variantId
            ? Variants?.FirstOrDefault(v => v.Id == variantId)?.Key
            : null;

    /// <summary>
    /// Определение вычислимого поля (для <see cref="MetaFieldType.Query"/>)
    /// </summary>
    public MetaFieldQueryDefinition? GetQueryDefinition()
        => MetaFieldQueryDefinition.FromOptions(Options);
}
