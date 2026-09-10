using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Forms.Abstractions;
using Mars.Forms.Abstractions.Services;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests.Validation;

/// <summary>
/// Встроенные правила — чистые функции общего слоя: их считают и клиент (мгновенная подсветка),
/// и серверный валидатор. Проверяется тот же код, что вызывает <c>FormFieldBinding.ValidationErrors</c>.
/// </summary>
public class FormRuleEvaluatorTests
{
    [Fact]
    public void Required_EmptyValues_ReportError()
    {
        Evaluate(Rule(FormRuleCatalog.Required), null).Should().Equal("поле обязательно");
        Evaluate(Rule(FormRuleCatalog.Required), "").Should().Equal("поле обязательно");
        Evaluate(Rule(FormRuleCatalog.Required), "abc").Should().BeEmpty();
    }

    [Fact]
    public void Length_ChecksBothBounds()
    {
        var rule = Rule(FormRuleCatalog.Length, ("min", 3), ("max", 5));

        Evaluate(rule, "ab").Should().Equal("минимальная длина 3");
        Evaluate(rule, "abcdef").Should().Equal("максимальная длина 5");
        Evaluate(rule, "abcd").Should().BeEmpty();
    }

    [Fact]
    public void Regex_SkipsEmptyValue_UsesCustomMessage()
    {
        var rule = Rule(FormRuleCatalog.Regex, ("pattern", "^[a-z]+$"), ("message", "только строчные"));

        Evaluate(rule, "").Should().BeEmpty();
        Evaluate(rule, "ABC").Should().Equal("только строчные");
        Evaluate(rule, "abc").Should().BeEmpty();
    }

    [Fact]
    public void MinMax_CompareNumbersAndStrings()
    {
        Evaluate(Rule(FormRuleCatalog.Min, ("value", 10)), 9).Should().Equal("значение должно быть не меньше 10");
        Evaluate(Rule(FormRuleCatalog.Max, ("value", 10)), "11").Should().Equal("значение должно быть не больше 10");
        Evaluate(Rule(FormRuleCatalog.Max, ("value", 10)), 10).Should().BeEmpty();
    }

    static IEnumerable<string> Evaluate(FormRuleDefinition rule, object? value)
        => FormRuleEvaluator.Evaluate(rule, value);

    static FormRuleDefinition Rule(string type, params (string Name, object Value)[] parameters)
    {
        var obj = new JsonObject();
        foreach (var (name, value) in parameters)
            obj[name] = value is string text ? JsonValue.Create(text) : JsonValue.Create(Convert.ToInt32(value));

        return new FormRuleDefinition { Type = type, Params = obj };
    }
}
