using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Mars.Core.Extensions;
using Mars.Host.Data.Common;
using Mars.Host.Data.Constants;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[DebuggerDisplay("{Type}/{Id}")]
public abstract class MetaValueBase : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    public EMetaFieldType Type { get; set; }
    public int Index { get; set; }

    public bool? Bool { get; set; }
    public int? Int { get; set; }
    public double? Float { get; set; }
    public decimal? Decimal { get; set; }
    public long? Long { get; set; }
    public string? StringText { get; set; }

    [StringLength(EntityDefaultConstants.DefaultShortValueMaxLength)]
    public string? StringShort { get; set; }

    public DateTime? DateTime { get; set; }

    public Guid? VariantId { get; set; }
    public Guid[] VariantsIds { get; set; } = [];

    public Guid? ModelId { get; set; }

    // ==========================================
    // Relations

    public Guid MetaFieldId { get; set; }
    public virtual MetaFieldEntity? MetaField { get; set; }

    #region SETTER

    public object? Get()
    {
        return MetaField.Type switch
        //return Type switch // проблема Type приходит 0 вместо нужного enum
        {
            EMetaFieldType.String => StringShort,
            EMetaFieldType.Text => StringText,

            EMetaFieldType.Bool => Bool,
            EMetaFieldType.Int => Int,
            EMetaFieldType.Long => Long,
            EMetaFieldType.Float => Float,
            EMetaFieldType.Decimal => Decimal,
            EMetaFieldType.DateTime => DateTime,

            EMetaFieldType.Select => MetaField.Variants.FirstOrDefault(s => s.Id == VariantId),
            EMetaFieldType.SelectMany => MetaField.Variants.Where(s => VariantsIds.Contains(s.Id)).ToArray(),

            EMetaFieldType.Relation => ModelId,
            EMetaFieldType.File => ModelId,
            EMetaFieldType.Image => ModelId,

            _ => throw new NotImplementedException()
        };
    }

    public void Set(MetaFieldEntity t, object? value)
    {
        //basic
        if (value is null)
        {
            if (t.IsNullable == false)
            {
                throw new ArgumentNullException(t.Key);
            }

            switch (t.Type)
            {
                case EMetaFieldType.String: StringShort = null; break;
                case EMetaFieldType.Text: StringText = null; break;
                case EMetaFieldType.Bool: Bool = null; break;
                case EMetaFieldType.Int: Int = null; break;
                case EMetaFieldType.Long: Long = null; break;
                case EMetaFieldType.Float: Float = null; break;
                case EMetaFieldType.Decimal: Decimal = null; break;
                case EMetaFieldType.DateTime: DateTime = null; break;
                case EMetaFieldType.Select: VariantId = null; break;
                case EMetaFieldType.SelectMany: VariantsIds = []; break;
                default:
                    if (ERelations.Contains(t.Type))
                    {
                        ModelId = null;
                    }
                    else
                    {
                        throw new NotImplementedException($"ArgumentException: type={t.Type} not implement");
                    }
                    break;
            }
        }
        else if (Type == EMetaFieldType.String && value is string _st)
        {
            StringShort = _st.Left(255);
        }
        else if (Type == EMetaFieldType.Text && value is string _text)
        {
            StringText = _text;
        }
        else if (Type == EMetaFieldType.Bool && value is bool _bool)
        {
            Bool = _bool;
        }
        else if (Type == EMetaFieldType.Int && value is int _int)
        {
            Int = _int;
        }
        else if (Type == EMetaFieldType.Long && value is long _long)
        {
            Long = _long;
        }
        else if (Type == EMetaFieldType.Float && value is double _float)
        {
            Float = _float;
        }
        else if (Type == EMetaFieldType.Decimal && value is decimal _decimal)
        {
            Decimal = _decimal;
        }
        //extra
        else if (Type == EMetaFieldType.DateTime && value is DateTime _date)
        {
            DateTime = _date;
        }
        else if (Type == EMetaFieldType.Select && value is Guid _variantId)
        {
            VariantId = _variantId;
        }
        else if (Type == EMetaFieldType.SelectMany && value is IEnumerable<Guid> _variantsIds)
        {
            VariantsIds = _variantsIds.ToArray();
        }
        else if (TypeRelation && value is Guid modelId)
        {
            ModelId = modelId;
        }
        else
        {
            throw new ArgumentException($"ArgumentException: template={t.Type} value set {value.GetType().Name}");
        }
    }

    public static string GetColName(EMetaFieldType type)
    {
        if (type == EMetaFieldType.String)
        {
            return nameof(StringShort);
        }
        else if (type == EMetaFieldType.Text)
        {
            return nameof(StringText);
        }
        else if (type == EMetaFieldType.Bool)
        {
            return nameof(Bool);
        }
        else if (type == EMetaFieldType.Int)
        {
            return nameof(Int);
        }
        else if (type == EMetaFieldType.Long)
        {
            return nameof(Long);
        }
        else if (type == EMetaFieldType.Float)
        {
            return nameof(Float);
        }
        else if (type == EMetaFieldType.Decimal)
        {
            return nameof(Decimal);
        }
        //extra
        else if (type == EMetaFieldType.DateTime)
        {
            return nameof(DateTime);
        }
        else if (type == EMetaFieldType.Select)
        {
            return nameof(VariantId);
        }
        else if (type == EMetaFieldType.SelectMany)
        {
            return nameof(VariantsIds);
        }
        //else if (type == EMetaFieldType.Relation)
        else if (MetaValueBase.ERelations.Contains(type))
        {
            return nameof(ModelId);
        }
        else
        {
            throw new NotImplementedException($"ArgumentException: type={type} not implement");
        }
    }

    #endregion

    #region ENUMS
    public static readonly EMetaFieldType[] ENumbers = { EMetaFieldType.Int, EMetaFieldType.Long, EMetaFieldType.Float, EMetaFieldType.Decimal };
    public static readonly EMetaFieldType[] EStrings = { EMetaFieldType.String, EMetaFieldType.Text };

    public static readonly EMetaFieldType[] EHasMinMax = {
        EMetaFieldType.String,
        EMetaFieldType.Text,
        EMetaFieldType.Int,
        EMetaFieldType.Long,
        EMetaFieldType.Float,
        EMetaFieldType.Decimal,
        EMetaFieldType.DateTime,
        EMetaFieldType.SelectMany,
    };
    public static readonly EMetaFieldType[] ESelectable = {
        EMetaFieldType.Select,
        EMetaFieldType.SelectMany,
    };

    public static readonly EMetaFieldType[] ERelations = {
        EMetaFieldType.Relation,
        EMetaFieldType.File,
        EMetaFieldType.Image,
    };

    //System.TypeCode

    public bool IsNumber => ENumbers.Contains(Type);
    public bool IsString => EStrings.Contains(Type);
    public bool TypeHasMinMax => EHasMinMax.Contains(Type);
    public bool TypeSelectable => ESelectable.Contains(Type);
    public bool TypeRelation => ERelations.Contains(Type);
    #endregion

    #region VALIDATE
    public IEnumerable<string> Check(object value)
    {
        if (MetaField is null) throw new ArgumentNullException("MetaFieldTemplate is null");

        return MetaValueBase.Check(MetaField, value);
    }

    public static IEnumerable<string> Check(MetaFieldEntity t, object value)
    {
        List<string> err = [];

        if (t.IsNullable == false && value is null)
        {
            err.Add("value cannot be null");
            return err;
        }

        if (t.TypeHasMinMax)
        {
            if (t.IsString && value is string _st)
            {
                if (t.MinValue is not null && _st.Length < t.MinValue) err.Add($"min length {t.MinValue}");
                if (t.MaxValue is not null && _st.Length > t.MaxValue) err.Add($"max length {t.MaxValue}");
            }

            if (t.IsNumber)
            {
                if (t.MinValue is not null && (dynamic)value < t.MinValue) err.Add($"min value {t.MinValue}");
                if (t.MaxValue is not null && (dynamic)value > t.MaxValue) err.Add($"max value {t.MinValue}");
            }
        }

        return err;
    }

    public bool IsValid(object value)
    {
        if (MetaField is null) throw new ArgumentNullException("MetaFieldTemplate is null");

        return MetaValueBase.IsValid(MetaField, value);
    }

    public static bool IsValid(MetaFieldEntity t, object value)
    {
        return Check(t, value).Count() == 0;
    }
    #endregion

    public T? Get<T>()
    {
        return (T?)Get();
    }

}
