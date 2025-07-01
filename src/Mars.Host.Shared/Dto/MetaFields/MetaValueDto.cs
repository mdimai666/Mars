using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

[DebuggerDisplay("{Type}={Value} /id={Id}")]
public record MetaValueDto
{
    public required Guid Id { get; init; }
    public required MetaFieldType Type { get; init; }
    public required int Index { get; init; }

    public required Guid? VariantId { get; init; }
    public required Guid[]? VariantsIds { get; init; }
    public required Guid? ModelId { get; init; }

    /// <summary>
    /// object may be
    /// <list type="bullet">
    /// <item><see cref="Type.IsPrimitive"/></item>
    /// <item><see cref="string"/></item>
    /// <item><see cref="MetaFieldVariantDto"/></item>
    /// <item><see cref="MetaFieldVariantDto"/>[]</item>
    /// </list>
    /// </summary>
    public required object? Value { get; init; }
    public required MetaFieldDto MetaField { get; init; }

}

public abstract record MetaValueDetailBase
{
    public required Guid Id { get; init; }
    public required Guid ParentId { get; set; }
    public required int Index { get; init; }

    public required bool Bool { get; set; }
    public required int Int { get; set; }
    public required float Float { get; set; }
    public required decimal Decimal { get; set; }
    public required long Long { get; set; }
    public required string? StringText { get; set; }

    [StringLength(256)]
    public required string? StringShort { get; set; }

    public required bool NULL { get; set; }

    public required DateTime DateTime { get; set; }

    public required Guid VariantId { get; set; }
    public required Guid[] VariantsIds { get; set; } = [];

    public required Guid ModelId { get; set; }

    public object? GetValueSimple(MetaFieldType type)
    {
        return type switch
        {
            MetaFieldType.String => StringShort,
            MetaFieldType.Text => StringText,

            MetaFieldType.Bool => Bool,
            MetaFieldType.Int => Int,
            MetaFieldType.Long => Long,
            MetaFieldType.Float => Float,
            MetaFieldType.Decimal => Decimal,
            MetaFieldType.DateTime => DateTime,

            MetaFieldType.Select => VariantId,
            MetaFieldType.SelectMany => VariantsIds,

            MetaFieldType.Group => null,//???

            MetaFieldType.Relation => ModelId,
            MetaFieldType.File => ModelId,
            MetaFieldType.Image => ModelId,

            _ => throw new NotImplementedException()
        };
    }
}

[DebuggerDisplay("{MetaField.Type}/id={Id}")]
public record MetaValueDetailDto : MetaValueDetailBase
{
    public required MetaFieldDto MetaField { get; init; }

}

public record ModifyMetaValueDetailDto : MetaValueDetailBase
{
    public required Guid MetaFieldId { get; init; }

}
