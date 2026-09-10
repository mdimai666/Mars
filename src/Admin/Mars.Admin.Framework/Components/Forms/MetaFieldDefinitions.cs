using System.Text.Json.Nodes;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Admin.Framework.Extensions;
using Mars.Cms.Contracts.MetaFields;
using Mars.Core.Features;
using Mars.Forms.Contracts;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Группа метаполей в общем редакторе определений: держит модели источника
/// (<see cref="MetaFieldEditModel"/> — то, что уходит в API) и их определения
/// (<see cref="FormFieldDefinition"/> — то, что правит редактор) и синхронизирует правки.
/// Общие параметры правит редактор (<see cref="Apply"/>), доменные панели настроек правят модель
/// источника напрямую (<see cref="Refresh"/>). Модель источника остаётся единственным хранилищем:
/// определение — его проекция, поэтому добавление/дублирование/удаление идут по списку полей.
/// </summary>
public class MetaFieldDefinitions
{
    /// <summary>Параметры правил метаполя: валидатор длины не принимает сообщение</summary>
    static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ValidatorParams =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [MetaFieldValidatorCatalog.Regex] = ["pattern", "message"],
            [MetaFieldValidatorCatalog.Length] = ["min", "max"],
        };

    readonly List<MetaFieldEditModel> _fields;
    readonly List<FormFieldDefinition> _definitions = [];
    readonly IMetaFieldEditorLocator _editors;

    public MetaFieldDefinitions(List<MetaFieldEditModel> fields, IMetaFieldEditorLocator editors)
    {
        _fields = fields;
        _editors = editors;
        Rebuild();
    }

    /// <summary>Ключ поля, на который указывает фича типа (картинка поста): нельзя удалить и сменить тип</summary>
    public string? FeatureFieldKey { get; set; }

    /// <summary>Фича «Контент» включена: поле с фиксированным ключом защищено и не переименовывается</summary>
    public bool ContentFeatureEnabled { get; set; }

    /// <summary>Поле переименовано (старый ключ, новый ключ) — владелец двигает указатель фичи</summary>
    public Action<string, string>? OnFieldKeyRenamed { get; set; }

    public IReadOnlyList<FormFieldDefinition> Definitions => _definitions;

    /// <summary>Пересобирает определения из списка полей (после смены фич типа, добавления, удаления)</summary>
    public void Rebuild()
    {
        var existing = _definitions.ToDictionary(definition => definition.Id);

        _definitions.Clear();
        foreach (var field in _fields.OrderBy(s => s.Order))
        {
            // экземпляр определения сохраняем: по нему строка редактора держит состояние (раскрыта/свёрнута)
            if (existing.TryGetValue(field.Id, out var definition))
            {
                Fill(definition, field);
                _definitions.Add(definition);
            }
            else
            {
                _definitions.Add(ToDefinition(field));
            }
        }
    }

    public void Add()
    {
        var order = _fields.Count > 0 ? _fields.Max(s => s.Order) + 1 : 0;
        _fields.Add(NewField(order));
        Rebuild();
    }

    public void Clone(FormFieldDefinition definition)
    {
        if (Source(definition) is not { } field) return;

        _fields.Add(field.Clone(_fields.Count));
        Rebuild();
    }

    public void Delete(FormFieldDefinition definition)
    {
        if (Source(definition) is not { } field || IsProtected(field)) return;

        _fields.Remove(field);
        Rebuild();
    }

    /// <summary>Правка общего параметра в редакторе → модель источника</summary>
    public void Apply(FormFieldDefinition definition)
    {
        if (Source(definition) is not { } field) return;

        if (definition.Key != field.Key)
        {
            var oldKey = field.Key;
            field.Key = definition.Key;
            OnFieldKeyRenamed?.Invoke(oldKey, field.Key);
        }

        field.Title = definition.Title;
        // ключ обязателен: пустой подбирается из заголовка, как в прежнем редакторе
        if (string.IsNullOrWhiteSpace(field.Key))
            field.Key = TextTool.TranslateToPostSlug(field.Title);

        field.Description = definition.Description ?? "";
        // массив тегов одной ссылкой: InputTags2 сравнивает значение по ссылке, копия зациклила бы рендер
        field.Tags = definition.Tags as string[] ?? [.. definition.Tags];
        field.IsNullable = !definition.Required;
        field.Hidden = definition.Hidden;
        field.Disabled = definition.Disabled;
        field.Order = definition.Order;
        field.MinValue = definition.Min;
        field.MaxValue = definition.Max;
        field.Editor = definition.Editor ?? "";
        field.ModelName = definition.ModelName ?? "";

        if (definition.Multiple != field.IsMultiple)
        {
            field.IsMultiple = definition.Multiple;
            // список объектов недоступен без кратности — выключение снимает вид поля
            if (!definition.Multiple) field.Kind = "";
        }

        ApplyRules(field, definition.Rules);
        Refresh(definition);
    }

    /// <summary>Модель источника изменилась (доменная панель, пикер типа) → определение</summary>
    public void Refresh(FormFieldDefinition definition)
    {
        if (Source(definition) is not { } field) return;

        Fill(definition, field);
    }

    public static MetaFieldEditModel NewField(int order) => new()
    {
        Id = Guid.NewGuid(),
        Order = order,
    };

    //=====================================

    FormFieldDefinition ToDefinition(MetaFieldEditModel field)
    {
        var definition = new FormFieldDefinition { Id = field.Id, Source = field };
        Fill(definition, field);
        return definition;
    }

    void Fill(FormFieldDefinition definition, MetaFieldEditModel field)
    {
        definition.Id = field.Id;
        definition.Source = field;
        definition.Key = field.Key;
        definition.Title = field.Title;
        definition.Description = field.Description;
        definition.Type = field.Type.ToFormFieldType();
        definition.TypeTitle = MetaFieldEditModel.TypeList[field.Type];
        definition.TypeIcon = MetaFieldEditModel.TypeIcons[field.Type];
        definition.Required = !field.IsNullable;
        definition.Multiple = field.IsMultiple;
        definition.SupportsMultiple = field.IsTypeRelation;
        definition.SupportsLimits = field.IsTypeHasMinMax;
        definition.Hidden = field.Hidden;
        definition.Disabled = field.Disabled;
        definition.Editor = field.Editor;
        definition.Min = field.MinValue;
        definition.Max = field.MaxValue;
        definition.ModelName = field.ModelName;
        definition.Tags = field.Tags;
        definition.Order = field.Order;
        definition.Options = field.Options;
        definition.Protected = IsProtected(field);
        definition.KeyLocked = IsContentFeatureField(field);
        definition.Editors = _editors.EditorsFor(field.Type);
        definition.RuleOptions = MetaFieldValidatorCatalog.For(field.Type);
        definition.RuleParams = ValidatorParams;
        definition.Rules = ToRules(field);
    }

    bool IsContentFeatureField(MetaFieldEditModel field)
        => ContentFeatureEnabled && field.Key == FeatureFieldsCatalog.ContentFieldKey;

    bool IsFeatureField(MetaFieldEditModel field)
        => FeatureFieldKey is not null && field.Key == FeatureFieldKey;

    /// <summary>Поле защищено фичей типа: нельзя удалить и сменить тип</summary>
    bool IsProtected(MetaFieldEditModel field) => IsFeatureField(field) || IsContentFeatureField(field);

    static MetaFieldEditModel? Source(FormFieldDefinition definition) => definition.Source as MetaFieldEditModel;

    /// <summary>Правила общего слоя → строки валидаторов метаполя (форма та же, что в Options.validators)</summary>
    static void ApplyRules(MetaFieldEditModel field, IReadOnlyCollection<FormRuleDefinition> rules)
    {
        field.Validators = rules.Select(rule => new MetaFieldEditModel.MetaFieldValidatorEditRow
        {
            Type = rule.Type,
            Pattern = ReadString(rule, "pattern") ?? "",
            Message = ReadString(rule, "message") ?? "",
            Min = ReadInt(rule, "min"),
            Max = ReadInt(rule, "max"),
        }).ToList();
    }

    /// <summary>Строки валидаторов метаполя → правила общего слоя; набор параметров совпадает с Options.validators</summary>
    static List<FormRuleDefinition> ToRules(MetaFieldEditModel field)
        => field.Validators.Select(row =>
        {
            var parameters = new JsonObject();

            if (row.Type == MetaFieldValidatorCatalog.Regex)
            {
                parameters["pattern"] = row.Pattern;
                if (!string.IsNullOrEmpty(row.Message)) parameters["message"] = row.Message;
            }
            else if (row.Type == MetaFieldValidatorCatalog.Length)
            {
                if (row.Min is int min) parameters["min"] = min;
                if (row.Max is int max) parameters["max"] = max;
            }

            return new FormRuleDefinition { Type = row.Type, Params = parameters };
        }).ToList();

    static string? ReadString(FormRuleDefinition rule, string name)
        => rule.Params?[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    static int? ReadInt(FormRuleDefinition rule, string name)
    {
        if (rule.Params?[name] is not JsonValue value) return null;
        if (value.TryGetValue<int>(out var number)) return number;
        return value.TryGetValue<string>(out var text) && int.TryParse(text, out var parsed) ? parsed : null;
    }
}
