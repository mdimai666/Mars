using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.MetaFields;

namespace AppFront.Shared.Components.MetaFieldViews;

/// <summary>
/// <see cref="MetaValueDetailResponse"/>
/// </summary>
public class MetaValueEditModel
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public int Index { get; set; }

    public bool Bool { get; set; }
    public int Int { get; set; }
    public float Float { get; set; }
    public decimal Decimal { get; set; }
    public long Long { get; set; }
    public string StringText { get; set; } = "";
    
    [StringLength(256)]
    public string StringShort { get; set; } = "";

    public bool NULL { get; set; }

    public DateTime? DateTime { get; set; }

    public Guid VariantId { get; set; }
    public Guid[] VariantsIds { get; set; } = [];

    public Guid ModelId { get; set; }

    public MetaFieldEditModel MetaField { get; init; } = default!;


    public CreateMetaValueRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            ParentId = ParentId,
            Index = Index,

            Bool = Bool,
            Int = Int,
            Float = Float,
            Decimal = Decimal,
            Long = Long,
            DateTime = DateTime ?? System.DateTime.MinValue,

            ModelId = ModelId,
            StringShort = StringShort,
            StringText = StringText,
            NULL = NULL,
            VariantId = VariantId,
            VariantsIds = VariantsIds,

            MetaFieldId = MetaField.Id,
        };

    public UpdateMetaValueRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            ParentId = ParentId,
            Index = Index,

            Bool = Bool,
            Int = Int,
            Float = Float,
            Decimal = Decimal,
            Long = Long,
            DateTime = DateTime ?? System.DateTime.MinValue,

            ModelId = ModelId,
            StringShort = StringShort,
            StringText = StringText,
            NULL = NULL,
            VariantId = VariantId,
            VariantsIds = VariantsIds,

            MetaFieldId = MetaField.Id,
        };

    public static MetaValueEditModel ToModel(MetaValueDetailResponse response)
        => new()
        {
            Id = response.Id,
            ParentId = response.ParentId,
            Index = response.Index,

            Bool = response.Bool,
            Int = response.Int,
            Float = response.Float,
            Decimal = response.Decimal,
            Long = response.Long,
            DateTime = response.DateTime == System.DateTime.MinValue ? null : response.DateTime,

            ModelId = response.ModelId,
            StringShort = response.StringShort ?? "",
            StringText = response.StringText ?? "",
            NULL = response.NULL,
            VariantId = response.VariantId,
            VariantsIds = response.VariantsIds ?? [],
            MetaField = MetaFieldEditModel.ToModel(response.MetaField!)
        };
}
