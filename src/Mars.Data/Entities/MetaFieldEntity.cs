using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Mars.Core.Extensions;
using Mars.Data.Common;
using Mars.Data.OwnedTypes.MetaFields;
using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Entities;

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

    [Comment("Множественное: поле допускает несколько значений")]
    public bool IsMultiple { get; set; }

    [Comment("Значение по умолчанию")]
    public virtual MetaFieldDefaultValue? Default { get; set; }

    [Comment("Опции (точка расширения)")]
    public JsonNode? Options { get; set; }

    [Comment("Порядок")]
    public int Order { get; set; }

    [Comment("Теги")]
    public List<string> Tags { get; set; } = [];

    [Comment("Скрытое: хранится и отдаётся в API, но скрыт в формах")]
    public bool Hidden { get; set; }

    [Comment("Отключен: исключён из генерации/форм, значения сохраняются")]
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

    // Поле принадлежит ровно одному типу (1:1 из трёх владельцев)

    public Guid? PostTypeId { get; set; }
    public virtual PostTypeEntity? PostType { get; set; }

    public Guid? UserTypeId { get; set; }
    public virtual UserTypeEntity? UserType { get; set; }

    public Guid? PostCategoryTypeId { get; set; }
    public virtual PostCategoryTypeEntity? PostCategoryType { get; set; }

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

                [EMetaFieldType.Query] = "Query",

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
            EMetaFieldType.Query => "Query",
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

            EMetaFieldType.Query => typeof(object),//вычислимое — значения не хранятся

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

    /// <summary>
    /// Вычислимое поле: хранится только определение, значение резолвится на чтении
    /// </summary>
    Query = 110,
}
