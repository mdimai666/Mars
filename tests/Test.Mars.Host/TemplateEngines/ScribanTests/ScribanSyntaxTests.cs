using FluentAssertions;
using Mars.TemplateEngine.Providers.ScribanProvider;

namespace Test.Mars.Host.TemplateEngines.ScribanTests;

public class ScribanSyntaxTests : ITemplateEngineSyntaxTests
{
    // 1. Тест: Вывод переменной
    [Fact]
    public void Render_VariableSubstitution_ReplacesPlaceholdersWithValues()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string template = "Имя: {{ User.Name }}, Возраст: {{ User.Age }}";
        var context = new
        {
            User = new { Name = "Дмитрий", Age = 28 }
        };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Имя: Дмитрий, Возраст: 28");
    }

    // 2. Тест: Условие (Блок {{ if }}) — сценарий TRUE
    [Fact]
    public void Render_IfConditionTrue_RendersTrueBlock()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string template = "{{ if IsActive }}Доступ открыт{{ else }}Доступ закрыт{{ end }}";
        var context = new { IsActive = true };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Доступ открыт");
    }

    // 3. Тест: Условие (Блок {{ if }}) — сценарий FALSE
    [Fact]
    public void Render_IfConditionFalse_RendersElseBlock()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string template = "{{ if IsActive }}Доступ открыт{{ else }}Доступ закрыт{{ end }}";
        var context = new { IsActive = false };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Доступ закрыт");
    }

    // 4. Тест: Цикл (Блок {{ for }} для массивов и списков)
    [Fact]
    public void Render_EachLoop_RendersAllItemsInCollection()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string template = "Элементы:{{ for item in Items }} [{{ item }}]{{ end }}";
        var context = new
        {
            Items = new List<string> { "Яблоко", "Банан", "Груша" }
        };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Элементы: [Яблоко] [Банан] [Груша]");
    }

    // 5. Тест: Доступность данных (Свойства, которых НЕТ в контексте)
    // Scriban по умолчанию не падает при отсутствии ключа, а выводит пустую строку.
    [Fact]
    public void Render_MissingPropertyInContext_OutputsEmptyStringWithoutCrashing()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string template = "Привет, {{ MissingProperty }}!";
        var context = new { ExistingProperty = "Тест" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Привет, !");
    }

    // 6. Тест: Доступ к глобальному/родительскому контексту изнутри цикла
    // В Scriban, в отличие от Handlebars, переменные из внешнего контекста видны внутри цикла напрямую.
    [Fact]
    public void Render_ParentContextInsideLoop_AllowsAccessToOuterPropertiesDirectly()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string template = "{{ for item in Items }}{{ Category }}: {{ item }}; {{ end }}";
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
