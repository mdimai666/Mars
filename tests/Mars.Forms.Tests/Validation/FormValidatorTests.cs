using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Forms.Abstractions.Services;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests.Validation;

public class FormValidatorTests
{
    readonly FormRuleRegistry _registry = new();
    readonly FormValidator _validator;

    public FormValidatorTests()
    {
        _validator = new FormValidator(_registry);
    }

    [Fact]
    public async Task RequiredField_MissingValue_AddsError()
    {
        var definition = Definition(Title(required: true));
        var values = new FormValues { OwnerModel = OwnerModel };

        var errors = await _validator.ValidateAsync(definition, values);

        errors.Should().ContainSingle().Which.Message.Should().Be("поле обязательно");
    }

    [Fact]
    public async Task RequiredRule_FromLayout_HasSameEffect()
    {
        var definition = Definition(Title(rules: Rule(FormRuleCatalog.Required)));
        var values = new FormValues { OwnerModel = OwnerModel, Values = { ["title"] = "" } };

        var errors = await _validator.ValidateAsync(definition, values);

        errors.Should().ContainSingle().Which.Key.Should().Be("title");
    }

    [Fact]
    public async Task OptionalField_EmptyValue_IsSkipped()
    {
        var definition = Definition(Title(rules: Rule(FormRuleCatalog.Regex, ("pattern", "^[a-z]+$"))));
        var values = new FormValues { OwnerModel = OwnerModel, Values = { ["title"] = "" } };

        var errors = await _validator.ValidateAsync(definition, values);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ShapeViolation_IsReportedBeforeRules()
    {
        var definition = Definition(Title(rules: Rule(FormRuleCatalog.Regex, ("pattern", "^[a-z]+$"))));
        var values = new FormValues { OwnerModel = OwnerModel, Values = { ["title"] = new JsonArray("a") } };

        var errors = await _validator.ValidateAsync(definition, values);

        errors.Should().ContainSingle().Which.Message.Should().Be("поле не множественное: ожидается одно значение");
    }

    [Fact]
    public async Task DescriptorLimits_ApplyToNumbers()
    {
        var field = new FormFieldDescriptor { Key = "price", Title = "Цена", Type = FormFieldType.Decimal, Min = 3 };
        var item = new FormItem { Key = "price", Field = field };
        var values = new FormValues { OwnerModel = OwnerModel, Values = { ["price"] = "2" } };

        var errors = await _validator.ValidateAsync(Definition(item), values);

        errors.Should().ContainSingle().Which.Message.Should().Be("значение должно быть не меньше 3");
    }

    [Fact]
    public async Task MinMaxRules_ReadLimitFromParams()
    {
        var field = new FormFieldDescriptor
        {
            Key = "count",
            Title = "Количество",
            Type = FormFieldType.Int,
            Rules = [Rule(FormRuleCatalog.Max, ("value", 10))],
        };
        var item = new FormItem { Key = "count", Field = field };
        var values = new FormValues { OwnerModel = OwnerModel, Values = { ["count"] = 42 } };

        var errors = await _validator.ValidateAsync(Definition(item), values);

        errors.Should().ContainSingle().Which.Message.Should().Contain("не больше 10");
    }

    [Fact]
    public async Task CustomMessage_FromParams_Wins()
    {
        var field = Title(rules: Rule(FormRuleCatalog.Length, ("min", 5), ("message", "слишком короткий заголовок")));
        var values = new FormValues { OwnerModel = OwnerModel, Values = { ["title"] = "ab" } };

        var errors = await _validator.ValidateAsync(Definition(new FormItem { Key = "title", Field = field }), values);

        errors.Should().ContainSingle().Which.Message.Should().Be("слишком короткий заголовок");
    }

    [Fact]
    public async Task MultipleField_ErrorsCarryElementIndex()
    {
        var field = new FormFieldDescriptor
        {
            Key = "tags",
            Title = "Теги",
            Type = FormFieldType.String,
            Multiple = true,
            Rules = [Rule(FormRuleCatalog.Length, ("min", 3))],
        };
        var item = new FormItem { Key = "tags", Field = field };
        var values = new FormValues { OwnerModel = OwnerModel, Values = { ["tags"] = new JsonArray("ab", "abc", "x") } };

        var errors = await _validator.ValidateAsync(Definition(item), values);

        errors.Select(e => e.Index).Should().Equal(0, 2);
        errors.Should().OnlyContain(e => e.Key == "tags");
    }

    [Fact]
    public async Task ReadOnlyAndComputedFields_AreSkipped()
    {
        var items = new[]
        {
            new FormItem
            {
                Key = "created_at",
                Field = new FormFieldDescriptor
                {
                    Key = "created_at",
                    Title = "Создан",
                    Type = FormFieldType.DateTime,
                    Required = true,
                    ReadOnly = true,
                },
            },
            new FormItem
            {
                Key = "back",
                Field = new FormFieldDescriptor { Key = "back", Title = "Обратная связь", Type = FormFieldType.Computed, Required = true },
            },
        };
        var values = new FormValues { OwnerModel = OwnerModel };

        var errors = await _validator.ValidateAsync(Definition(items), values);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task UnknownRule_IsSoftlySkipped()
    {
        var item = new FormItem { Key = "title", Field = Title(rules: Rule("sqlNotNull")) };
        var values = new FormValues { OwnerModel = OwnerModel, Values = { ["title"] = "abc" } };

        var errors = await _validator.ValidateAsync(Definition(item), values);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ScopedRule_OfAnotherProvider_IsNotApplied()
    {
        _registry.Register("sql.*", "notNull", (_, _, _, _) => ValueTask.FromResult<IEnumerable<string>>(["не null"]));
        var item = new FormItem { Key = "title", Field = Title(rules: Rule("notNull")) };
        var values = new FormValues { OwnerModel = OwnerModel, Values = { ["title"] = "abc" } };

        var errors = await _validator.ValidateAsync(Definition(item), values);

        errors.Should().BeEmpty();
    }

    const string OwnerModel = "post.article";

    static FormDefinition Definition(params FormItem[] items) => Definition((IEnumerable<FormItem>)items);

    static FormDefinition Definition(params FormFieldDescriptor[] fields)
        => Definition(fields.Select(f => new FormItem { Key = f.Key, Field = f }));

    static FormDefinition Definition(IEnumerable<FormItem> items)
        => new() { OwnerModel = OwnerModel, Items = items.ToList() };

    static FormFieldDescriptor Title(bool required = false, params FormRuleDefinition[] rules) => new()
    {
        Key = "title",
        Title = "Заголовок",
        Type = FormFieldType.String,
        Required = required,
        Rules = rules,
    };

    static FormRuleDefinition Rule(string type, params (string Name, object Value)[] parameters)
    {
        var obj = new JsonObject();
        foreach (var (name, value) in parameters)
            obj[name] = value is string text ? JsonValue.Create(text) : JsonValue.Create(Convert.ToInt32(value));

        return new FormRuleDefinition { Type = type, Params = obj };
    }
}
