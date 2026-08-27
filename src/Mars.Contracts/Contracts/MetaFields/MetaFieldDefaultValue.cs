namespace Mars.Contracts.MetaFields;

/// <summary>
/// Значение мета-поля по умолчанию (хранится на определении поля).
/// </summary>
public record MetaFieldDefaultValue
{
    public bool? Bool { get; init; }
    public int? Int { get; init; }
    public double? Float { get; init; }
    public decimal? Decimal { get; init; }
    public long? Long { get; init; }
    public string? StringText { get; init; }
    public string? StringShort { get; init; }
    public DateTime? DateTime { get; init; }
    public Guid? VariantId { get; init; }
    public Guid[]? VariantsIds { get; init; }
    public Guid? ModelId { get; init; }
}
