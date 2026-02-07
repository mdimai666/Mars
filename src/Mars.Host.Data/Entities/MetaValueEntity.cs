using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Mars.Core.Extensions;
using Mars.Host.Data.Common;
using Mars.Host.Data.Constants;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[DebuggerDisplay("{Type}/{Id}")]
public class MetaValueEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    public Guid ParentId { get; set; }
    public EMetaFieldType Type { get; set; }
    public int Index { get; set; }

    public bool Bool { get; set; }
    public int Int { get; set; }
    public float Float { get; set; }
    public decimal Decimal { get; set; }
    public long Long { get; set; }
    public string? StringText { get; set; }

    [StringLength(EntityDefaultConstants.DefaultShortValueMaxLength)]
    public string? StringShort { get; set; }

    public bool NULL { get; set; }

    public DateTime DateTime { get; set; }

    public Guid VariantId { get; set; }
    public Guid[] VariantsIds { get; set; } = [];

    public Guid ModelId { get; set; }

    // ==========================================
    // Relations

    [ForeignKey(nameof(MetaField))]
    public Guid MetaFieldId { get; set; }
    public virtual MetaFieldEntity? MetaField { get; set; }

    public virtual ICollection<PostMetaValueEntity>? PostMetaValues { get; set; }

    [NotMapped]
    public virtual List<PostEntity>? Posts { get; set; }

    public virtual ICollection<UserMetaValueEntity>? UserMetaValues { get; set; }

    [NotMapped]
    public virtual List<UserEntity>? Users { get; set; }

    public virtual ICollection<PostCategoryMetaValueEntity>? PostCategoryMetaValues { get; set; }

    [NotMapped]
    public virtual List<PostCategoryEntity>? PostCategories { get; set; }

    #region SETTER

    //DateTime

    //public void Set(object value)
    //{
    //    if (value is bool _bool)
    //    {
    //        Bool = _bool;
    //    }
    //    else if (value is int _int)
    //    {
    //        Int = _int;
    //    }
    //    //========
    //    if (value is string Str)
    //    {

    //    }
    //    //========
    //    else if (value is float _float)
    //    {
    //        Float = _float;
    //    }
    //    else if (value is decimal _decimal)
    //    {
    //        Decimal = _decimal;
    //    }
    //    else if (value is long _long)
    //    {
    //        Long = _long;
    //    }
    //    else
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public object? Get()
    {
        if (NULL) return default;

        return MetaField.Type switch
        //return Type switch // проблема Type приходит 0 вместо нужного enum
        {
            EMetaFieldType.String => StringShort ?? "",
            EMetaFieldType.Text => StringText ?? "",

            EMetaFieldType.Bool => Bool,
            EMetaFieldType.Int => Int,
            EMetaFieldType.Long => Long,
            EMetaFieldType.Float => Float,
            EMetaFieldType.Decimal => Decimal,
            EMetaFieldType.DateTime => DateTime,

            EMetaFieldType.Select => MetaField.Variants.FirstOrDefault(s => s.Id == VariantId),
            EMetaFieldType.SelectMany => MetaField.Variants.Where(s => VariantsIds.Contains(s.Id)).ToArray(),

            EMetaFieldType.Group => null,//???

            EMetaFieldType.Relation => ModelId,
            EMetaFieldType.File => ModelId,
            EMetaFieldType.Image => ModelId,

            _ => throw new NotImplementedException()
        };
    }

    public void Set(MetaFieldEntity t, object value)
    {
        //basic
        if (value is null)
        {
            if (t.IsNullable)
            {
                NULL = true;
            }
            else
            {
                throw new ArgumentNullException(t.Key);
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
        else if (Type == EMetaFieldType.Float && value is float _float)
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
        else if (MetaValueEntity.ERelations.Contains(type))
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
        EMetaFieldType.List,
    };
    public static readonly EMetaFieldType[] ESelectable = {
        EMetaFieldType.Select,
        EMetaFieldType.SelectMany,
    };
    public static readonly EMetaFieldType[] EParentable = {
        EMetaFieldType.Group,
        EMetaFieldType.List,
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
    public bool TypeParentable => EParentable.Contains(Type);
    public bool TypeRelation => ERelations.Contains(Type);
    #endregion

    #region VALIDATE
    public IEnumerable<string> Check(object value)
    {
        if (MetaField is null) throw new ArgumentNullException("MetaFieldTemplate is null");

        return MetaValueEntity.Check(MetaField, value);
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

            if (t.Type == EMetaFieldType.List)
            {
                throw new NotImplementedException();
            }
        }

        return err;
    }

    public bool IsValid(object value)
    {
        if (MetaField is null) throw new ArgumentNullException("MetaFieldTemplate is null");

        return MetaValueEntity.IsValid(MetaField, value);
    }

    public static bool IsValid(MetaFieldEntity t, object value)
    {
        return Check(t, value).Count() == 0;
    }
    #endregion

    public T? Get<T>()
    {
        if (NULL) return default;

        return (T?)Get();
    }

}

public class MetaValueTree
{
    public string Key { get; set; } = string.Empty;
    public EMetaFieldType Type { get; set; }
    public object Value { get; set; } = default!;
    public bool IsList { get; set; }
    //public int Index { get; set; }
    public IEnumerable<MetaValueTree> Childs { get; set; } = [];

    public MetaValueTree()
    {

    }

    public MetaValueTree(MetaValueEntity value)
    {
        Key = value.MetaField.Key;
        Type = value.Type;
        Value = (value.Type == EMetaFieldType.List ? null : value.Get())!;
        IsList = value.Type == EMetaFieldType.List;
    }

    public MetaValueTree(MetaFieldEntity value)
    {
        Key = value.Key;
        Type = value.Type;
        IsList = value.Type == EMetaFieldType.List;
    }

}
