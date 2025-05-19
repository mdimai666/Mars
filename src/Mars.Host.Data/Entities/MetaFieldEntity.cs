using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Mars.Host.Data.Common;
using Mars.Host.Data.Configurations;
using Mars.Host.Data.OwnedTypes.MetaFields;
using Mars.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[DebuggerDisplay("{Type}/{Id}/{Title}")]
[EntityTypeConfiguration(typeof(MetaFieldEntityConfiguration))]
public class MetaFieldEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }


    [Comment("Родительское мета поле")]
    public Guid ParentId { get; set; }

    [Comment("Название")]
    public string Title { get; set; } = default!;

    [Required]
    [Comment("Key")]
    public string Key { get; set; } = default!;

    [Comment("Тип")]
    public EMetaFieldType Type { get; set; } = EMetaFieldType.String;

    [Column(TypeName = "jsonb")]
    [Comment("Варианты")]
    public List<MetaFieldVariant> Variants { get; set; } = new();

    [Comment("Максимальное")]
    public decimal? MaxValue { get; set; } = null;
    [Comment("Минимальное")]
    public decimal? MinValue { get; set; } = null;

    [Comment("Описание")]
    public string Description { get; set; } = "";

    [Comment("IsNullable")]
    public bool IsNullable { get; set; }

    [NotMapped]
    [Column(TypeName = "jsonb")]
    [Comment("Значение по умолчанию")]
    public MetaValueEntity? Default { get; set; }

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

    public virtual ICollection<MetaValueEntity>? MetaValues { get; set; }



    public virtual ICollection<PostTypeMetaFieldEntity>? PostTypeMetaFields { get; set; }
    [NotMapped]
    public virtual List<PostTypeEntity>? PostTypes { get; set; }


    public virtual ICollection<UserMetaFieldEntity>? UserMetaFields { get; set; }
    [NotMapped]
    public virtual List<UserEntity>? Users { get; set; }


    #region ENUMS
    public static readonly EMetaFieldType[] ENumbers = MetaValueEntity.ENumbers;
    public static readonly EMetaFieldType[] EStrings = MetaValueEntity.EStrings;

    public static readonly EMetaFieldType[] EHasMinMax = MetaValueEntity.EHasMinMax;
    public static readonly EMetaFieldType[] ESelectable = MetaValueEntity.ESelectable;
    public static readonly EMetaFieldType[] EParentable = MetaValueEntity.EParentable;
    public static readonly EMetaFieldType[] ERelations = MetaValueEntity.ERelations;

    public bool IsNumber => ENumbers.Contains(Type);
    public bool IsString => EStrings.Contains(Type);
    public bool TypeHasMinMax => EHasMinMax.Contains(Type);
    public bool TypeSelectable => ESelectable.Contains(Type);
    public bool TypeParentable => EParentable.Contains(Type);
    public bool TypeRelation => ERelations.Contains(Type);

    #endregion


    #region TYPE_LIST
    static Dictionary<EMetaFieldType, string> _typeList = null;


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

                [EMetaFieldType.Group] = "Группа",
                [EMetaFieldType.List] = "List",

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
            EMetaFieldType.Group => "Группа",
            EMetaFieldType.List => "List",
            _ => type.ToString()
        };
    }
    #endregion

    //Validators


    public static ICollection<MetaValueEntity> GetValuesBlank(ICollection<MetaValueEntity> metaValues, ICollection<MetaFieldEntity> metaFields)
    {

        if (metaValues is null || metaFields is null)
        {
            throw new ArgumentException();
        }

        //var existDict = metaValues.ToDictionary(s => s.MetaFieldId);

        var blankValues = FieldsBlank(metaValues, /*existDict, */metaFields, Guid.Empty);
        return metaValues.Concat(blankValues).Where(s => !s.MetaField.Disabled).ToList();
    }

    public static ICollection<MetaValueEntity> FieldsBlank(ICollection<MetaValueEntity> metaValues, ICollection<MetaFieldEntity> metaFields, Guid parentId, int index = 0)
    {
        List<MetaValueEntity> list = new();

        foreach (var f in metaFields.Where(s => s.ParentId == parentId && !s.Disabled))
        {
            //bool exist = existDict.ContainsKey(f.Id);
            var value = metaValues.FirstOrDefault(s => s.MetaFieldId == f.Id && s.Index == index);
            bool exist = value is not null;

            if (!exist)
            //{
            //    list.Add(value);
            //}
            //else
            {
                if (f.TypeParentable == false)
                {
                    var val = new MetaValueEntity
                    {
                        MetaFieldId = f.Id,
                        MetaField = f,
                        Type = f.Type,
                        //PostId = a.Id,
                        NULL = f.IsNullable,
                        ParentId = parentId,
                    };

                    list.Add(val);
                }
            }

            if (f.TypeParentable)
            {

                if (f.Type == EMetaFieldType.Group)
                {
                    var values = FieldsBlank(metaValues, metaFields, f.Id);
                    list.AddRange(values);

                }
                else if (f.Type == EMetaFieldType.List)
                {

                    var childs = metaValues.Where(s => s.ParentId == f.Id);

                    if (childs.Count() == 0)
                    {
                        //Console.WriteLine("222");

                        //var group = new MetaValue
                        //{
                        //    Id = Guid.NewGuid(),
                        //    MetaFieldId = f.Id,
                        //    MetaField = f,
                        //    Type = EMetaFieldType.Group,
                        //    //PostId = a.Id,
                        //    ParentId = parentId,
                        //};

                        //list.Add(group);

                        var values = FieldsBlank(metaValues, metaFields, f.Id);
                        //values.Where(s => s.ParentId == f.Id).ToList().ForEach(s => s.ParentId = group.Id);
                        list.AddRange(values);


                        //var metaList = f;
                        //var iteratorMetaFields = metaFields.Where(s => s.ParentId == metaList.Id).ToList();
                        //var items = MetaField.MetaListGetNewGroupChild(metaList, metaFields);
                        //list.AddRange(items);
                    }
                    else
                    {
                        var groups = childs.GroupBy(s => s.Index).OrderBy(s => s.Key);

                        foreach (var group in groups)
                        {
                            var values = FieldsBlank(metaValues, metaFields, f.Id, group.Key);
                            list.AddRange(values);

                        }

                    }
                }
            }


        }
        list = list.Where(s => s.MetaField is not null).OrderBy(s => s.MetaField.Order).ToList();

        return list;
    }

    public static ICollection<MetaValueEntity> MetaListGetNewGroupChild(MetaFieldEntity list, ICollection<MetaFieldEntity> metaFields, int index)
    {
        //List<MetaValue> values = new();

        //var group = new MetaValue
        //{
        //    Id = Guid.NewGuid(),
        //    MetaFieldId = list.Id,
        //    MetaField = list,
        //    Type = EMetaFieldType.Group,
        //    //PostId = a.Id,
        //    ParentId = list.Id,
        //};

        //values.Add(group);

        //var x = FieldsBlank(new List<MetaValue>(), metaFields, list.Id);
        //foreach (var f in x)
        //{
        //    f.ParentId = group.Id;
        //}

        //values.AddRange(x);

        //return values;

        var values = FieldsBlank(new List<MetaValueEntity>(), metaFields, list.Id);
        foreach (var v in values.Where(s => s.ParentId == list.Id))
        {
            v.Index = index;
        }
        return values;
    }

    public static Type MetaFieldTypeToType(EMetaFieldType mtype)
    {
        return mtype switch
        {
            EMetaFieldType.String => typeof(string),
            EMetaFieldType.Text => typeof(string),
            EMetaFieldType.Bool => typeof(bool),
            EMetaFieldType.Int => typeof(int),
            EMetaFieldType.Long => typeof(long),
            EMetaFieldType.Float => typeof(float),
            EMetaFieldType.Decimal => typeof(decimal),
            EMetaFieldType.DateTime => typeof(DateTime),

            EMetaFieldType.Select => typeof(Guid), //typeof(MetaFieldVariant),
            EMetaFieldType.SelectMany => typeof(Guid[]), //typeof(List<MetaFieldVariant>),

            EMetaFieldType.Group => throw new NotImplementedException(),//todo: implement
            EMetaFieldType.List => throw new NotImplementedException(),

            EMetaFieldType.Relation => typeof(Guid),//IBasicEntity
            EMetaFieldType.File => typeof(Guid),//FileEntity
            EMetaFieldType.Image => typeof(Guid),//FileEntity

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

    Group = 40,
    List = 50,

    Relation = 100,
    File = 101,
    Image = 102,
}


public class FieldScheme//not use
{
    public enum Type
    {
        Once,
        List,
        Group,

        Relation// to some other model
    }
}


//Repeater
//Groups
//select from other models

//public class MetaFieldDto//придумать
//{

//}


//public class ManyToMany_Post_MetaForm : BasicEntityNonUser
//{
//    [ForeignKey(nameof(Post))]
//    public virtual Guid PostId { get; set; }
//    public virtual Post Post { get; set; }

//    [ForeignKey(nameof(MetaForm))]
//    public virtual Guid MetaFormId { get; set; }
//    public virtual MetaForm MetaForm { get; set; }
//}

//public interface IManyToManyCompaitable<TManyToManyEntity>//придумать
//{
//    (Expression<Func<TManyToManyEntity, bool>> queryExp,
//    Expression<Func<TManyToManyEntity, Guid>> mainEntitySelect,
//    Expression<Func<TManyToManyEntity, Guid>> subEntitySelect,
//    Guid postId)
//    GetManyToManyMTuple();
//}

//[Table("MetaForms")]
//public class MetaForm : BasicEntityNonUser
//{
//    [Comment("Название")]
//    public string Title { get; set; }
//    [Comment("Key")]
//    public string Key { get; set; }
//    [Comment("Описание")]
//    public string Description { get; set; } = "";

//    public virtual ICollection<MetaFieldTemplate> MetaFieldTemplates { get; set; }
//}


