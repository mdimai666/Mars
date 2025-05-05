namespace Mars.Shared.Contracts.MetaFields;

public record MetaValueResponse
{
    public required Guid Id { get; init; }
    //public required int Index { get; init; }

    //public required Guid? VariantId { get; init; }
    //public required Guid[]? VariantsIds { get; init; }
    //public required Guid? ModelId { get; init; }

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
    //public required MetaFieldResponse? MetaField { get; init; }
}
