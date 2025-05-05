using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

public record MetaFieldDto
{
    public required Guid Id { get; init; }
    public required Guid ParentId { get; init; }
    public required string Title { get; init; }
    public required string Key { get; init; }
    public required MetaFieldType Type { get; init; }

    public required decimal? MaxValue { get; init; }
    public required decimal? MinValue { get; init; }
    public required string Description { get; init; }
    public required bool IsNullable { get; init; }
    //public required MetaValueDto? Default { get; init; }
    public required int Order { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required bool Hidden { get; init; }
    public required bool Disabled { get; init; }

    public required IReadOnlyCollection<MetaFieldVariantDto>? Variants { get; init; }
    public required string? ModelName { get; init; }

    public bool IsTypeParentable => Type is MetaFieldType.List or MetaFieldType.Group;
    public bool IsTypeRelation => Type is MetaFieldType.Relation or MetaFieldType.File or MetaFieldType.Image;
}
