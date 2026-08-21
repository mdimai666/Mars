using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Mars.Core.Extensions;
using Mars.Host.Data.Common;
using Mars.Host.Data.OwnedTypes.MetaFields;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[DebuggerDisplay("{Type}/{Id}/{Title}")]
public class MetaFieldEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Comment("Название")]
    public string Title { get; set; } = default!;

    [Required]
    [Comment("Key")]
    public string Key { get; set; } = default!;

    [Comment("Тип")]
    public EMetaFieldType Type { get; set; } = EMetaFieldType.String;

    [Comment("Варианты")]
    public virtual List<MetaFieldVariant> Variants { get; set; } = [];

    [Comment("Максимальное")]
    public decimal? MaxValue { get; set; } = null;
    [Comment("Минимальное")]
    public decimal? MinValue { get; set; } = null;

    [Comment("Описание")]
    public string Description { get; set; } = "";

    [Comment("IsNullable")]
    public bool IsNullable { get; set; }

    [NotMapped]
    [Comment("Значение по умолчанию")]
    public MetaValueBase? Default { get; set; }

    [Comment("Порядок")]
    public int Order { get; set; }

    [Comment("Теги")]
    public List<string> Tags { get; set; } = [];

    [Comment("Скрытое")]
    public bool Hidden { get; set; }

    [Comment("Отключен")]
    public bool Disabled { get; set; }

    ////SETTERS===============
    //[Comment("Теги")]
    ////[JsonIgnore]
    //[NotMapped]
    //public virtual IEnumerable<string> SetTags { get => Tags ?? new(); set => Tags = value.ToList(); }
    //SETTERS===============

    /////////////////////////////////////
    /// <summary>
    /// <seealso cref="MetaField.GetModelType(EMetaFieldType,string)"/>
    /// </summary>
    [Comment("Модель")]
    public string? ModelName { get; set; }

    //SETTERS===============

    // Relations

    public virtual ICollection<PostMetaValueEntity>? PostMetaValues { get; set; }
    public virtual ICollection<UserMetaValueEntity>? UserMetaValues { get; set; }
    public virtual ICollection<PostCategoryMetaValueEntity>? PostCategoryMetaValues { get; set; }

    public virtual ICollection<PostTypeMetaFieldEntity>? PostTypeMetaFields { get; set; }

    [NotMapped]
    public virtual List<PostTypeEntity>? PostTypes { get; set; }

    public virtual ICollection<UserTypeMetaFieldEntity>? UserTypeMetaFields { get; set; }

    [NotMapped]
    public virtual List<UserTypeEntity>? UserTypes { get; set; }

    public virtual ICollection<PostCategoryTypeMetaFieldEntity>? PostCategoryTypeMetaFields { get; set; }

    [NotMapped]
    public virtual List<PostCategoryTypeEntity>? PostCategoryTypes { get; set; }

    #region ENUMS
    public static readonly EMetaFieldType[] ENumbers = MetaValueBase.ENumbers;
    public static readonly EMetaFieldType[] EStrings = MetaValueBase.EStrings;

    public static readonly EMetaFieldType[] EHasMinMax = MetaValueBase.EHasMinMax;
    public static readonly EMetaFieldType[] ESelectable = MetaValueBase.ESelectable;
    public static readonly EMetaFieldType[] ERelations = MetaValueBase.ERelations;

    public bool IsNumber => ENumbers.Contains(Type);
    public bool IsString => EStrings.Contains(Type);
    public bool TypeHasMinMax => EHasMinMax.Contains(Type);
    public bool TypeSelectable => ESelectable.Contains(Type);
    public bool TypeRelation => ERelations.Contains(Type);

    #endregion

    #region TYPE_LIST
    static Dictionary<EMetaFieldType, string>? _typeList = null;

    [Comment("Тип поля")]
    [NotMapped]
    public static Dictionary<EMetaFieldType, string> TypeList
    {
        get
        {
            if (_typeList != null) return _typeList;

            _typeList = new Dictionary<EMetaFieldType, string>()
            {
                [EMetaFieldType.String] = "Строка(255)",
                [EMetaFieldType.Text] = "Текст",
                [EMetaFieldType.Bool] = "Да/Нет",

                [EMetaFieldType.Int] = EMetaFieldType.Int.ToString(),
                [EMetaFieldType.Long] = EMetaFieldType.Long.ToString(),
                [EMetaFieldType.Float] = EMetaFieldType.Float.ToString(),
                [EMetaFieldType.Decimal] = EMetaFieldType.Decimal.ToString(),

                [EMetaFieldType.DateTime] = "Дата",

                [EMetaFieldType.Select] = "Выбор",
                [EMetaFieldType.SelectMany] = "Выбор из многих",

                [EMetaFieldType.Relation] = "Relation",
                [EMetaFieldType.File] = "File",
                [EMetaFieldType.Image] = "Image",

            };

            return _typeList;
        }
    }

    public string TypeAsText()
    {
        return TypeAsText(Type);
    }
    public static string TypeAsText(EMetaFieldType type)
    {
        return type switch
        {
            EMetaFieldType.String => "Строка(255)",
            EMetaFieldType.Text => "Текст",
            EMetaFieldType.Bool => "Да/Нет",
            EMetaFieldType.Select => "Выбор",
            EMetaFieldType.SelectMany => "Выбор из многих",
            EMetaFieldType.DateTime => "Дата",
            _ => type.ToString()
        };
    }
    #endregion

    public static Type MetaFieldTypeToType(EMetaFieldType mtype)
    {
        return mtype switch
        {
            EMetaFieldType.String => typeof(string),
            EMetaFieldType.Text => typeof(string),
            EMetaFieldType.Bool => typeof(bool?),
            EMetaFieldType.Int => typeof(int?),
            EMetaFieldType.Long => typeof(long?),
            EMetaFieldType.Float => typeof(double?),
            EMetaFieldType.Decimal => typeof(decimal?),
            EMetaFieldType.DateTime => typeof(DateTime?),

            EMetaFieldType.Select => typeof(Guid), //typeof(MetaFieldVariant),
            EMetaFieldType.SelectMany => typeof(Guid[]), //typeof(List<MetaFieldVariant>),

            EMetaFieldType.Relation => typeof(Guid?),//IBasicEntity
            EMetaFieldType.File => typeof(Guid?),//FileEntity
            EMetaFieldType.Image => typeof(Guid?),//FileEntity

            _ => throw new NotImplementedException()
        };
    }
}

public enum EMetaFieldType : int
{
    /// <summary>
    /// short string (255)
    /// </summary>
    String = TypeCode.String,
    Text = 28,
    Bool = TypeCode.Boolean,
    Int = TypeCode.Int32,
    Long = TypeCode.Int64,
    Float = TypeCode.Single,
    Decimal = TypeCode.Decimal,
    DateTime = TypeCode.DateTime,

    Select = 30,
    SelectMany = 31,

    Relation = 100,
    File = 101,
    Image = 102,
}
