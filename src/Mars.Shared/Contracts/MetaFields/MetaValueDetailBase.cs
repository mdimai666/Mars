using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.Contracts.MetaFields;

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
    public required Guid[] VariantsIds { get; init; }

    public required Guid ModelId { get; init; }

}
