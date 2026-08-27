using System.ComponentModel.DataAnnotations;
using Mars.Contracts.MetaFields;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// <see cref="MetaValueDetailResponse"/>
/// </summary>
public class MetaValueEditModel
{
    public Guid Id { get; set; }
    public int Index { get; set; }

    /// <summary>Ключ мета-поля (стабильный идентификатор значения в словаре <c>(Key, Index)</c>)</summary>
    public string Key => MetaField.Key;

    public bool Bool { get; set; }
    public int Int { get; set; }
    public double Float { get; set; }
    public decimal Decimal { get; set; }
    public long Long { get; set; }
    public string StringText { get; set; } = "";

    [StringLength(256)]
    public string StringShort { get; set; } = "";

    public DateTime? DateTime { get; set; }

    public Guid VariantId { get; set; }
    public Guid[] VariantsIds { get; set; } = [];

    public Guid ModelId { get; set; }

    public MetaFieldEditModel MetaField { get; init; } = default!;


    public CreateMetaValueRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            Index = Index,

            Bool = Bool,
            Int = Int,
            Float = Float,
            Decimal = Decimal,
            Long = Long,
            DateTime = DateTime,

            ModelId = ModelId == Guid.Empty ? null : ModelId,
            StringShort = StringShort,
            StringText = StringText,
            VariantId = VariantId == Guid.Empty ? null : VariantId,
            VariantsIds = VariantsIds,

            MetaFieldId = MetaField.Id,
        };

    public UpdateMetaValueRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            Index = Index,

            Bool = Bool,
            Int = Int,
            Float = Float,
            Decimal = Decimal,
            Long = Long,
            DateTime = DateTime,

            ModelId = ModelId == Guid.Empty ? null : ModelId,
            StringShort = StringShort,
            StringText = StringText,
            VariantId = VariantId == Guid.Empty ? null : VariantId,
            VariantsIds = VariantsIds,

            MetaFieldId = MetaField.Id,
        };

    public static MetaValueEditModel ToModel(MetaValueDetailResponse response)
        => new()
        {
            Id = response.Id,
            Index = response.Index,

            Bool = response.Bool ?? false,
            Int = response.Int ?? 0,
            Float = response.Float ?? 0,
            Decimal = response.Decimal ?? 0,
            Long = response.Long ?? 0,
            DateTime = response.DateTime,

            ModelId = response.ModelId ?? Guid.Empty,
            StringShort = response.StringShort ?? "",
            StringText = response.StringText ?? "",
            VariantId = response.VariantId ?? Guid.Empty,
            VariantsIds = response.VariantsIds ?? [],
            MetaField = MetaFieldEditModel.ToModel(response.MetaField!)
        };
}
