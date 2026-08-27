using FluentAssertions;
using Mars.TemplateEngine.Providers.HandlebarsProvider;

namespace Test.Mars.Host.TemplateEngines.HandlebarsTests;

public class HandlebarsSyntaxTests : ITemplateEngineSyntaxTests
{
    // 1. Тест: Вывод переменной
    [Fact]
    public void Render_VariableSubstitution_ReplacesPlaceholdersWithValues()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();
        string template = "Имя: {{User.Name}}, Возраст: {{User.Age}}";
        var context = new
        {
            User = new { Name = "Дмитрий", Age = 28 }
        };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Имя: Дмитрий, Возраст: 28");
    }

    // 2. Тест: Условие (Блок {{#if}}) — сценарий TRUE
    [Fact]
    public void Render_IfConditionTrue_RendersTrueBlock()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();
        string template = "{{#if IsActive}}Доступ открыт{{else}}Доступ закрыт{{/if}}";
        var context = new { IsActive = true };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Доступ открыт");
    }

    // 3. Тест: Условие (Блок {{#if}}) — сценарий FALSE
    [Fact]
    public void Render_IfConditionFalse_RendersElseBlock()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();
        string template = "{{#if IsActive}}Доступ открыт{{else}}Доступ закрыт{{/if}}";
        var context = new { IsActive = false };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Доступ закрыт");
    }

    // 4. Тест: Цикл (Блок {{#each}} для массивов и списков)
    [Fact]
    public void Render_EachLoop_RendersAllItemsInCollection()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();
        string template = "Элементы:{{#each Items}} [{{this}}]{{/each}}";
        var context = new
        {
            Items = new List<string> { "Яблоко", "Банан", "Груша" }
        };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Элементы: [Яблоко] [Банан] [Груша]");
    }

    // 5. Тест: Доступность данных в шаблоне (Свойства, которых НЕТ в контексте)
    // Handlebars по умолчанию не падает, если переменная отсутствует, а выводит пустую строку.
    [Fact]
    public void Render_MissingPropertyInContext_OutputsEmptyStringWithoutCrashing()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();
        string template = "Привет, {{MissingProperty}}!";
        var context = new { ExistingProperty = "Тест" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Привет, !");
    }

    // 6. Тест: Доступ к родительскому контексту изнутри цикла (Оператор ../)
    [Fact]
    public void Render_ParentContextInsideLoop_AllowsAccessToOuterProperties()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();
        string template = "{{#each Items}}{{../Category}}: {{this}}; {{/each}}";
        var context = new
        {
            Category = "Фрукты",
            Items = new[] { "Ананас", "Киви" }
        };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Фрукты: Ананас; Фрукты: Киви; ");
    }
}
