using System.ComponentModel.DataAnnotations;

namespace Mars.Host.Data.OwnedTypes.MetaFields;

/// <summary>
/// OWNED Значение мета-поля по умолчанию (хранится на определении поля, owned-json).
/// </summary>
public class MetaFieldDefaultValue
{
    public bool? Bool { get; set; }
    public int? Int { get; set; }
    public double? Float { get; set; }
    public decimal? Decimal { get; set; }
    public long? Long { get; set; }
    public string? StringText { get; set; }

    [MaxLength(256)]
    public string? StringShort { get; set; }

    public DateTime? DateTime { get; set; }

    public Guid? VariantId { get; set; }
    public Guid[]? VariantsIds { get; set; }

    public Guid? ModelId { get; set; }
}
