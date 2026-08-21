using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;
using Mars.Core.Attributes;
using Mars.Shared.Contracts.MetaFields;

namespace AppFront.Shared.Components.MetaFieldViews;

/// <summary>
/// <see cref="MetaFieldDetailResponse"/>
/// </summary>
public class MetaFieldEditModel
{
    public Guid Id { get; set; }

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

    public MetaFieldDefaultValue? Default { get; set; }
    public JsonNode? Options { get; set; }

    /// <summary>Поле ещё не сохранялось на сервер (нет значений, о которых нужно предупреждать).</summary>
    public bool IsNew { get; set; } = true;

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
            Title = Title,
            Key = Key,
            Type = Type,
            MaxValue = MaxValue,
            MinValue = MinValue,
            Description = Description,
            IsNullable = IsNullable,
            Default = Default,
            Options = Options,
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
        Title = Title,
        Key = Key,
        Type = Type,
        MaxValue = MaxValue,
        MinValue = MinValue,
        Description = Description,
        IsNullable = IsNullable,
        Default = Default,
        Options = Options,
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
        Title = response.Title,
        Key = response.Key,
        Type = response.Type,
        MaxValue = response.MaxValue,
        MinValue = response.MinValue,
        Description = response.Description,
        IsNullable = response.IsNullable,
        Default = response.Default,
        Options = response.Options,
        IsNew = false,
        Order = response.Order,
        Tags = response.Tags.ToArray(),
        Hidden = response.Hidden,
        Disabled = response.Disabled,
        Variants = response.Variants?.Select(MetaFieldVariantEditModel.ToModel).ToList() ?? [],
        ModelName = response.ModelName ?? "",
    };

    /// <summary>«Создать поле из существующего»: копия с новыми Id (поле и варианты).</summary>
    public MetaFieldEditModel Clone(int order)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = Title,
            Key = $"{Key}_copy",
            Type = Type,
            MaxValue = MaxValue,
            MinValue = MinValue,
            Description = Description,
            IsNullable = IsNullable,
            Default = Default,
            Options = Options,
            IsNew = true,
            Order = order,
            Tags = [.. Tags],
            Hidden = Hidden,
            Disabled = Disabled,
            Variants = Variants.Select(s => s.Clone()).ToList(),
            ModelName = ModelName,
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
    };
    public static readonly MetaFieldType[] ESelectable = {
        MetaFieldType.Select,
        MetaFieldType.SelectMany,
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
    public bool IsTypeRelation => ERelations.Contains(Type);
    public bool IsTypeQuery => Type == MetaFieldType.Query;
    #endregion

    #region QUERY_OPTIONS
    /// <summary>Определение вычислимого поля: целевой тип (формат ModelName)</summary>
    public string QueryTarget
    {
        get => ReadOptionString("target");
        set => WriteOptionString("target", value);
    }

    /// <summary>Определение вычислимого поля: ключ Relation-поля цели, ссылающегося на этот тип</summary>
    public string QueryBackReferenceKey
    {
        get => ReadOptionString("backReferenceKey");
        set => WriteOptionString("backReferenceKey", value);
    }

    string ReadOptionString(string name)
        => Options is JsonObject obj && obj[name] is JsonValue value && value.TryGetValue<string>(out var result)
            ? result
            : "";

    void WriteOptionString(string name, string optionValue)
    {
        if (Options is not JsonObject obj)
        {
            obj = new JsonObject();
            Options = obj;
        }
        obj[name] = optionValue;
    }
    #endregion

    public string Label => TypeList[Type];

    #region TYPE_LIST
    static Dictionary<MetaFieldType, string>? _typeList = null;

    [Display(Name = "Тип поля")]
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

                [MetaFieldType.Query] = "Query",

            };

            return _typeList;
        }
    }
    #endregion

    #region TYPE_ICONS
    static Dictionary<MetaFieldType, string>? _typeIcons = null;

    [Display(Name = "Тип icon")]
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

                [MetaFieldType.Query] = "🧮",

            };

            return _typeIcons;
        }
    }
    #endregion
}
