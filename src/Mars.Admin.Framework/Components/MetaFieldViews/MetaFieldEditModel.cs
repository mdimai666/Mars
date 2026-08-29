using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;
using Mars.Cms.Contracts.MetaFields;
using Mars.Core.Attributes;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

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

    /// <summary>false = обязательное поле (по умолчанию новые поля необязательные)</summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>Поле допускает несколько значений (дефолт — одинарное)</summary>
    public bool IsMultiple { get; set; } = false;

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
    {
        SyncValidatorsToOptions();
        SyncGeneratorToOptions();
        SyncEditorToOptions();
        SyncCodeLangToOptions();
        SyncKindToOptions();
        return new()
        {
            Id = Id,
            Title = Title,
            Key = Key,
            Type = Type,
            MaxValue = MaxValue,
            MinValue = MinValue,
            Description = Description,
            IsNullable = IsNullable,
            IsMultiple = IsMultiple,
            Default = Default,
            Options = Options,
            Order = Order,
            Tags = Tags,
            Hidden = Hidden,
            Disabled = Disabled,
            Variants = Variants?.Select(s => s.ToCreateRequest()).ToList(),
            ModelName = ModelName,
        };
    }

    public UpdateMetaFieldRequest ToUpdateRequest()
    {
        SyncValidatorsToOptions();
        SyncGeneratorToOptions();
        SyncEditorToOptions();
        SyncCodeLangToOptions();
        SyncKindToOptions();
        return new()
        {
            Id = Id,
            Title = Title,
            Key = Key,
            Type = Type,
            MaxValue = MaxValue,
            MinValue = MinValue,
            Description = Description,
            IsNullable = IsNullable,
            IsMultiple = IsMultiple,
            Default = Default,
            Options = Options,
            Order = Order,
            Tags = Tags,
            Hidden = Hidden,
            Disabled = Disabled,
            Variants = Variants?.Select(s => s.ToUpdateRequest()).ToList(),
            ModelName = ModelName,
        };
    }

    public static MetaFieldEditModel ToModel(MetaFieldDetailResponse response)
    {
        var model = new MetaFieldEditModel
        {
            Id = response.Id,
            Title = response.Title,
            Key = response.Key,
            Type = response.Type,
            MaxValue = response.MaxValue,
            MinValue = response.MinValue,
            Description = response.Description,
            IsNullable = response.IsNullable,
            IsMultiple = response.IsMultiple,
            Default = response.Default,
            Options = response.Options,
            IsNew = false,
            Order = response.Order,
            Tags = response.Tags.ToArray(),
            Hidden = response.Hidden,
            Disabled = response.Disabled,
            Variants = response.Variants?.Select(MetaFieldVariantEditModel.ToModel).ToList() ?? [],
            ModelName = response.ModelName ?? "",
            Validators = ReadValidators(response.Options),
        };
        ReadGenerator(model, response.Options);
        model.Editor = ReadEditor(response.Options);
        model.CodeLang = ReadCodeLang(response.Options);
        model.Kind = response.Options.GetKind();
        model.RemoveMode = response.Options.GetRemoveMode();
        model.ViewMode = response.Options.GetViewMode();
        model.DropZoneEnabled = response.Options.IsDropZoneEnabled();
        return model;
    }

    public MetaFieldEditModel Clone(int order)
    {
        var clone = new MetaFieldEditModel
        {
            Id = Guid.NewGuid(),
            Title = Title,
            Key = $"{Key}_copy",
            Type = Type,
            MaxValue = MaxValue,
            MinValue = MinValue,
            Description = Description,
            IsNullable = IsNullable,
            IsMultiple = IsMultiple,
            Default = Default,
            Options = Options.WithoutFeatureKey(),
            IsNew = true,
            Order = order,
            Tags = [.. Tags],
            Hidden = Hidden,
            Disabled = Disabled,
            Variants = Variants.Select(s => s.Clone()).ToList(),
            ModelName = ModelName,
            Validators = ReadValidators(Options),
        };
        ReadGenerator(clone, Options);
        clone.Editor = ReadEditor(Options);
        clone.CodeLang = ReadCodeLang(Options);
        clone.Kind = Options.GetKind();
        clone.RemoveMode = Options.GetRemoveMode();
        clone.ViewMode = Options.GetViewMode();
        clone.DropZoneEnabled = Options.IsDropZoneEnabled();
        return clone;
    }

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

    #region VALIDATORS
    /// <summary>Правила валидации значений (хранятся в Options.validators)</summary>
    public List<MetaFieldValidatorEditRow> Validators { get; set; } = [];

    public class MetaFieldValidatorEditRow
    {
        public string Type { get; set; } = MetaFieldValidatorCatalog.Regex;
        public string Pattern { get; set; } = "";
        public string Message { get; set; } = "";
        public int? Min { get; set; }
        public int? Max { get; set; }
    }

    static List<MetaFieldValidatorEditRow> ReadValidators(JsonNode? options)
    {
        var rows = new List<MetaFieldValidatorEditRow>();
        if (options is not JsonObject obj || obj["validators"] is not JsonArray array) return rows;

        foreach (var item in array.OfType<JsonObject>())
        {
            var type = item["type"] is JsonValue typeValue && typeValue.TryGetValue<string>(out var t) ? t : "";
            var p = item["params"] as JsonObject;
            rows.Add(new MetaFieldValidatorEditRow
            {
                Type = type,
                Pattern = p?["pattern"] is JsonValue pv && pv.TryGetValue<string>(out var pattern) ? pattern : "",
                Message = p?["message"] is JsonValue mv && mv.TryGetValue<string>(out var message) ? message : "",
                Min = p?["min"] is JsonValue minValue && minValue.TryGetValue<int>(out var min) ? min : null,
                Max = p?["max"] is JsonValue maxValue && maxValue.TryGetValue<int>(out var max) ? max : null,
            });
        }

        return rows;
    }

    /// <summary>Синхронизирует строки редактора в Options.validators</summary>
    public void SyncValidatorsToOptions()
    {
        if (Validators.Count == 0)
        {
            if (Options is JsonObject emptyObj) emptyObj.Remove("validators");
            return;
        }

        var array = new JsonArray();
        foreach (var row in Validators)
        {
            var p = new JsonObject();
            if (row.Type == MetaFieldValidatorCatalog.Regex)
            {
                p["pattern"] = row.Pattern;
                if (!string.IsNullOrEmpty(row.Message)) p["message"] = row.Message;
            }
            else if (row.Type == MetaFieldValidatorCatalog.Length)
            {
                if (row.Min is int min) p["min"] = min;
                if (row.Max is int max) p["max"] = max;
            }

            array.Add(new JsonObject { ["type"] = row.Type, ["params"] = p });
        }

        if (Options is not JsonObject obj)
        {
            obj = [];
            Options = obj;
        }
        obj["validators"] = array;
    }
    #endregion

    #region GENERATORS
    /// <summary>Генератор значения при создании (хранится в Options.generator)</summary>
    public string GeneratorType { get; set; } = "";
    public string GeneratorPrefix { get; set; } = "";
    public int GeneratorPaddingWidth { get; set; } = 4;
    public string GeneratorMode { get; set; } = MetaFieldGeneratorCatalog.ModeContinue;

    /// <summary>Словарь категория→префикс для генератора «порядковый номер»</summary>
    public List<MetaFieldGeneratorCategoryRow> GeneratorCategoryPrefixes { get; set; } = [];

    public class MetaFieldGeneratorCategoryRow
    {
        public string CategorySlug { get; set; } = "";
        public string Prefix { get; set; } = "";
    }

    static void ReadGenerator(MetaFieldEditModel model, JsonNode? options)
    {
        if (options is not JsonObject obj || obj["generator"] is not JsonObject generator) return;

        model.GeneratorType = generator["type"] is JsonValue typeValue && typeValue.TryGetValue<string>(out var type) ? type : "";

        if (generator["params"] is not JsonObject p) return;

        model.GeneratorPrefix = p["prefix"] is JsonValue prefixValue && prefixValue.TryGetValue<string>(out var prefix) ? prefix : "";
        model.GeneratorPaddingWidth = p["paddingWidth"] is JsonValue paddingValue && paddingValue.TryGetValue<int>(out var padding) ? padding : 4;
        model.GeneratorMode = p["mode"] is JsonValue modeValue && modeValue.TryGetValue<string>(out var mode)
            ? mode
            : MetaFieldGeneratorCatalog.ModeContinue;

        if (p["categoryPrefixes"] is JsonObject categoryPrefixes)
        {
            foreach (var (slug, node) in categoryPrefixes)
            {
                if (node is JsonValue v && v.TryGetValue<string>(out var categoryPrefix))
                    model.GeneratorCategoryPrefixes.Add(new MetaFieldGeneratorCategoryRow { CategorySlug = slug, Prefix = categoryPrefix });
            }
        }
    }

    /// <summary>Синхронизирует настройки генератора в Options.generator</summary>
    public void SyncGeneratorToOptions()
    {
        if (string.IsNullOrEmpty(GeneratorType))
        {
            if (Options is JsonObject emptyObj) emptyObj.Remove("generator");
            return;
        }

        var generator = new JsonObject { ["type"] = GeneratorType };

        if (GeneratorType == MetaFieldGeneratorCatalog.Sequence)
        {
            var p = new JsonObject
            {
                ["prefix"] = GeneratorPrefix,
                ["paddingWidth"] = GeneratorPaddingWidth,
                ["mode"] = GeneratorMode,
            };

            var categoryPrefixes = new JsonObject();
            foreach (var row in GeneratorCategoryPrefixes.Where(s => !string.IsNullOrWhiteSpace(s.CategorySlug)))
                categoryPrefixes[row.CategorySlug.Trim()] = row.Prefix;

            if (categoryPrefixes.Count > 0) p["categoryPrefixes"] = categoryPrefixes;

            generator["params"] = p;
        }

        if (Options is not JsonObject obj)
        {
            obj = [];
            Options = obj;
        }
        obj["generator"] = generator;
    }
    #endregion

    #region EDITOR
    /// <summary>Редактор значения (хранится в Options.editor); пусто = дефолтный редактор типа</summary>
    public string Editor { get; set; } = "";

    /// <summary>Язык кода для редактора Код (хранится в Options.codeLang); пусто = дефолтный</summary>
    public string CodeLang { get; set; } = "";

    static string ReadEditor(JsonNode? options) => options.GetEditor();

    static string ReadCodeLang(JsonNode? options)
        => options is JsonObject obj && obj[MetaFieldEditorCatalog.CodeLangOption()] is JsonValue value
            && value.TryGetValue<string>(out var lang)
            ? lang
            : "";

    /// <summary>Синхронизирует выбранный редактор в Options.editor</summary>
    public void SyncEditorToOptions()
    {
        if (string.IsNullOrEmpty(Editor))
        {
            if (Options is JsonObject emptyObj) emptyObj.Remove(MetaFieldEditorCatalog.EditorOption());
            return;
        }

        if (Options is not JsonObject obj)
        {
            obj = [];
            Options = obj;
        }
        obj[MetaFieldEditorCatalog.EditorOption()] = Editor;
    }

    /// <summary>Синхронизирует язык кода в Options.codeLang</summary>
    public void SyncCodeLangToOptions()
    {
        if (string.IsNullOrEmpty(CodeLang))
        {
            if (Options is JsonObject emptyObj) emptyObj.Remove(MetaFieldEditorCatalog.CodeLangOption());
            return;
        }

        if (Options is not JsonObject obj)
        {
            obj = [];
            Options = obj;
        }
        obj[MetaFieldEditorCatalog.CodeLangOption()] = CodeLang;
    }
    #endregion

    #region KIND
    /// <summary>Вид поля (хранится в Options.kind); пусто = обычный рендер по типу</summary>
    public string Kind { get; set; } = "";

    /// <summary>Удаление значений списка (хранится в Options.removeMode); пусто = по видимости типа-цели</summary>
    public string RemoveMode { get; set; } = "";

    /// <summary>Вид отображения списка детей (хранится в Options.viewMode); пусто = таблица</summary>
    public string ViewMode { get; set; } = "";

    /// <summary>Дроп-зона в редакторах значений (хранится в Options.dropZone); дефолт включена</summary>
    public bool DropZoneEnabled { get; set; } = true;

    /// <summary>Поле — упорядоченный список объектов (дочерних постов)</summary>
    public bool IsListKind => Kind == MetaFieldKindCatalog.List;

    /// <summary>Синхронизирует вид, режим удаления, вид отображения и дроп-зону в Options</summary>
    public void SyncKindToOptions()
    {
        if (Options is not JsonObject obj)
        {
            obj = [];
            Options = obj;
        }

        if (string.IsNullOrEmpty(Kind)) obj.Remove(MetaFieldKindCatalog.KindOption());
        else obj[MetaFieldKindCatalog.KindOption()] = Kind;

        if (string.IsNullOrEmpty(RemoveMode)) obj.Remove(MetaFieldKindCatalog.RemoveModeOption());
        else obj[MetaFieldKindCatalog.RemoveModeOption()] = RemoveMode;

        if (string.IsNullOrEmpty(ViewMode)) obj.Remove(MetaFieldKindCatalog.ViewModeOption());
        else obj[MetaFieldKindCatalog.ViewModeOption()] = ViewMode;

        if (DropZoneEnabled) obj.Remove(MetaFieldKindCatalog.DropZoneOption());
        else obj[MetaFieldKindCatalog.DropZoneOption()] = false;

        if (obj.Count == 0) Options = null;
    }
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

    /// <summary>Папка загрузки для полей Файл/Изображение (хранится в Options); пусто = папка года</summary>
    public string UploadFolder
    {
        get => ReadOptionString(MetaFieldKindCatalog.UploadFolderOption());
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (Options is JsonObject obj)
                {
                    obj.Remove(MetaFieldKindCatalog.UploadFolderOption());
                    if (obj.Count == 0) Options = null;
                }
                return;
            }

            WriteOptionString(MetaFieldKindCatalog.UploadFolderOption(), value.Trim());
        }
    }

    string ReadOptionString(string name)
        => Options is JsonObject obj && obj[name] is JsonValue value && value.TryGetValue<string>(out var result)
            ? result
            : "";

    void WriteOptionString(string name, string optionValue)
    {
        if (Options is not JsonObject obj)
        {
            obj = [];
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
