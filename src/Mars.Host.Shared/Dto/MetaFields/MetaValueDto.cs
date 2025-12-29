using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Mars.Core.Interfaces;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

[DebuggerDisplay("{Type}={Value} /id={Id}")]
public record MetaValueDto : IHasId
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
    public required Guid ParentId { get; init; }
    public required int Index { get; init; }

    public required bool Bool { get; init; }
    public required int Int { get; init; }
    public required float Float { get; init; }
    public required decimal Decimal { get; init; }
    public required long Long { get; init; }
    public required string? StringText { get; init; }

    [StringLength(256)]
    public required string? StringShort { get; init; }

    public required bool NULL { get; init; }

    public required DateTime DateTime { get; init; }

    public required Guid VariantId { get; init; }
    public required Guid[] VariantsIds { get; init; } = [];

    public required Guid ModelId { get; init; }

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
