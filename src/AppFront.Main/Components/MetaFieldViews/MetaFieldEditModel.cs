using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mars.Core.Attributes;
using Mars.Shared.Contracts.MetaFields;
using Microsoft.EntityFrameworkCore;

namespace AppFront.Shared.Components.MetaFieldViews;

/// <summary>
/// <see cref="MetaFieldDetailResponse"/>
/// </summary>
public class MetaFieldEditModel
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }

    [Required]
    [MinLength(2)]
    public string Title { get; set; } = "";

    [Required]
    [SlugString]
    [MinLength(2)]
    public string Key { get; set; } = "";
    public MetaFieldType Type { get; set; } = MetaFieldType.String;

    public decimal? MaxValue { get; set; }
    public decimal? MinValue { get; set; }
    public string Description { get; set; } = "";
    public bool IsNullable { get; set; }

    public int Order { get; set; }
    public string[] Tags { get; set; } = [];
    public bool Hidden { get; set; }
    public bool Disabled { get; set; }

    [ValidateComplexType]
    public List<MetaFieldVariantEditModel> Variants { get; set; } = [];
    public string ModelName { get; set; } = "";

    public CreateMetaFieldRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            ParentId = ParentId,
            Title = Title,
            Key = Key,
            Type = Type,
            MaxValue = MaxValue,
            MinValue = MinValue,
            Description = Description,
            IsNullable = IsNullable,
            //Default = //Default,
            Order = Order,
            Tags = Tags,
            Hidden = Hidden,
            Disabled = Disabled,
            Variants = Variants?.Select(s => s.ToCreateRequest()).ToList(),
            ModelName = ModelName,
        };

    public UpdateMetaFieldRequest ToUpdateRequest()
    => new()
    {
        Id = Id,
        ParentId = ParentId,
        Title = Title,
        Key = Key,
        Type = Type,
        MaxValue = MaxValue,
        MinValue = MinValue,
        Description = Description,
        IsNullable = IsNullable,
        //Default = //Default,
        Order = Order,
        Tags = Tags,
        Hidden = Hidden,
        Disabled = Disabled,
        Variants = Variants?.Select(s => s.ToUpdateRequest()).ToList(),
        ModelName = ModelName,
    };

    public static MetaFieldEditModel ToModel(MetaFieldDetailResponse response)
    => new()
    {
        Id = response.Id,
        ParentId = response.ParentId,
        Title = response.Title,
        Key = response.Key,
        Type = response.Type,
        MaxValue = response.MaxValue,
        MinValue = response.MinValue,
        Description = response.Description,
        IsNullable = response.IsNullable,
        //Default response.= //Default,
        Order = response.Order,
        Tags = response.Tags.ToArray(),
        Hidden = response.Hidden,
        Disabled = response.Disabled,
        Variants = response.Variants?.Select(MetaFieldVariantEditModel.ToModel).ToList() ?? [],
        ModelName = response.ModelName ?? "",
    };

    #region ENUMS
    public static readonly MetaFieldType[] ENumbers = { MetaFieldType.Int, MetaFieldType.Long, MetaFieldType.Float, MetaFieldType.Decimal };
    public static readonly MetaFieldType[] EStrings = { MetaFieldType.String, MetaFieldType.Text };

    public static readonly MetaFieldType[] EHasMinMax = {
        MetaFieldType.String,
        MetaFieldType.Text,
        MetaFieldType.Int,
        MetaFieldType.Long,
        MetaFieldType.Float,
        MetaFieldType.Decimal,
        MetaFieldType.DateTime,
        MetaFieldType.SelectMany,
        MetaFieldType.List,
    };
    public static readonly MetaFieldType[] ESelectable = {
        MetaFieldType.Select,
        MetaFieldType.SelectMany,
    };
    public static readonly MetaFieldType[] EParentable = {
        MetaFieldType.Group,
        MetaFieldType.List,
    };

    public static readonly MetaFieldType[] ERelations = {
        MetaFieldType.Relation,
        MetaFieldType.File,
        MetaFieldType.Image,
    };

    //System.TypeCode

    public bool IsNumber => ENumbers.Contains(Type);
    public bool IsString => EStrings.Contains(Type);
    public bool IsTypeHasMinMax => EHasMinMax.Contains(Type);
    public bool IsTypeSelectable => ESelectable.Contains(Type);
    public bool IsTypeParentable => EParentable.Contains(Type);
    public bool IsTypeRelation => ERelations.Contains(Type);
    #endregion

    public string Label => TypeList[Type];

    #region TYPE_LIST
    static Dictionary<MetaFieldType, string>? _typeList = null;

    [Comment("Тип поля")]
    [NotMapped]
    public static Dictionary<MetaFieldType, string> TypeList
    {
        get
        {
            if (_typeList != null) return _typeList;

            _typeList = new Dictionary<MetaFieldType, string>()
            {
                [MetaFieldType.String] = "Строка(255)",
                [MetaFieldType.Text] = "Текст",
                [MetaFieldType.Bool] = "Да/Нет",

                [MetaFieldType.Int] = MetaFieldType.Int.ToString(),
                [MetaFieldType.Long] = MetaFieldType.Long.ToString(),
                [MetaFieldType.Float] = MetaFieldType.Float.ToString(),
                [MetaFieldType.Decimal] = MetaFieldType.Decimal.ToString(),

                [MetaFieldType.DateTime] = "Дата",

                [MetaFieldType.Select] = "Выбор",
                [MetaFieldType.SelectMany] = "Выбор из многих",

                [MetaFieldType.Relation] = "Relation",
                [MetaFieldType.File] = "File",
                [MetaFieldType.Image] = "Image",

                [MetaFieldType.Group] = "Группа",
                [MetaFieldType.List] = "List",

            };

            return _typeList;
        }
    }
    #endregion

    #region TYPE_ICONS
    static Dictionary<MetaFieldType, string>? _typeIcons = null;

    [Comment("Тип icon")]
    [NotMapped]
    public static Dictionary<MetaFieldType, string> TypeIcons
    {
        get
        {
            if (_typeIcons != null) return _typeIcons;

            _typeIcons = new Dictionary<MetaFieldType, string>()
            {
                [MetaFieldType.String] = "🔤",
                [MetaFieldType.Text] = "🔡",
                [MetaFieldType.Bool] = "✅",

                [MetaFieldType.Int] = "🔢",
                [MetaFieldType.Long] = "🔢",
                [MetaFieldType.Float] = "🔢",
                [MetaFieldType.Decimal] = "💵",

                [MetaFieldType.DateTime] = "📅",

                [MetaFieldType.Select] = "✔️",
                [MetaFieldType.SelectMany] = "✔️✔️",

                [MetaFieldType.Relation] = "♦️",
                [MetaFieldType.File] = "📁",
                [MetaFieldType.Image] = "🖼️",

                [MetaFieldType.Group] = "⚙️",
                [MetaFieldType.List] = "🧰",

            };

            return _typeIcons;
        }
    }
    #endregion

    #region HELPERS
    public static IEnumerable<MetaValueEditModel> MetaListGetNewGroupChild(MetaFieldEditModel list, ICollection<MetaFieldEditModel> metaFields, int index)
    {
        var values = FieldsBlank(new List<MetaValueEditModel>(), metaFields, list.Id);
        foreach (var v in values.Where(s => s.ParentId == list.Id))
        {
            v.Index = index;
        }
        return values;
    }

    public static ICollection<MetaValueEditModel> FieldsBlank(ICollection<MetaValueEditModel> metaValues, ICollection<MetaFieldEditModel> metaFields, Guid parentId, int index = 0)
    {
        List<MetaValueEditModel> list = new();

        foreach (var f in metaFields.Where(s => s.ParentId == parentId && !s.Disabled))
        {
            //bool exist = existDict.ContainsKey(f.Id);
            var value = metaValues.FirstOrDefault(s => s.MetaField.Id == f.Id && s.Index == index);
            bool exist = value is not null;

            if (!exist)
            //{
            //    list.Add(value);
            //}
            //else
            {
                if (f.IsTypeParentable == false)
                {
                    var val = new MetaValueEditModel
                    {
                        Id = Guid.NewGuid(),
                        //MetaFieldId = f.Id,
                        MetaField = f,
                        //Type = f.Type,
                        //PostId = a.Id,
                        NULL = f.IsNullable,
                        ParentId = parentId,
                    };

                    list.Add(val);
                }
            }

            if (f.IsTypeParentable)
            {

                if (f.Type == MetaFieldType.Group)
                {
                    var values = FieldsBlank(metaValues, metaFields, f.Id);
                    list.AddRange(values);

                }
                else if (f.Type == MetaFieldType.List)
                {

                    var childs = metaValues.Where(s => s.ParentId == f.Id);

                    if (childs.Count() == 0)
                    {
                        ////Console.WriteLine("222");

                        ////var group = new MetaValue
                        ////{
                        ////    Id = Guid.NewGuid(),
                        ////    MetaFieldId = f.Id,
                        ////    MetaField = f,
                        ////    Type = EMetaFieldType.Group,
                        ////    //PostId = a.Id,
                        ////    ParentId = parentId,
                        ////};

                        ////list.Add(group);

                        //var values = FieldsBlank(metaValues, metaFields, f.Id);
                        ////values.Where(s => s.ParentId == f.Id).ToList().ForEach(s => s.ParentId = group.Id);
                        //list.AddRange(values);


                        ////var metaList = f;
                        ////var iteratorMetaFields = metaFields.Where(s => s.ParentId == metaList.Id).ToList();
                        ////var items = MetaField.MetaListGetNewGroupChild(metaList, metaFields);
                        ////list.AddRange(items);
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
    #endregion
}
