using FluentAssertions;
using Mars.TemplateEngine.Providers.ScribanProvider;

namespace Test.Mars.Host.TemplateEngines.ScribanTests;

public class ScribanTemplateEngineTests : ITemplateEngineInterfaceTests
{
    // Тест 1: Обычный рендеринг (Без кэша)
    [Fact]
    public void Render_WithValidTemplateAndContext_ReturnsExpectedString()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string template = "Привет, {{ Name }}! Твой баланс: {{ Balance }} руб.";
        var context = new { Name = "Иван", Balance = 150 };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Привет, Иван! Твой баланс: 150 руб.");
    }

    // Тест 2: Рендеринг с использованием кэша (Первый и повторный вызов)
    [Fact]
    public void RenderCached_CalledMultipleTimes_ReturnsCorrectResult()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string templateId = "user_welcome";
        string template = "Welcome, {{ User }}!";
        var context = new { User = "Alice" };

        // Act
        string firstResult = engine.RenderCached(templateId, template, context);
        string secondResult = engine.RenderCached(templateId, template, context);

        // Assert
        firstResult.Should().Be("Welcome, Alice!");
        secondResult.Should().Be("Welcome, Alice!");
    }

    // Тест 3: Удаление конкретного шаблона из кэша
    [Fact]
    public void RemoveFromCache_ExistingId_RemovesTemplateAndReturnsTrue()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string templateId = "temporary_template";
        string template = "Value: {{ Value }}";
        var context = new { Value = "Test" };

        engine.RenderCached(templateId, template, context);

        // Act
        bool isRemoved = engine.RemoveFromCache(templateId);
        bool isRemovedAgain = engine.RemoveFromCache(templateId);

        // Assert
        isRemoved.Should().BeTrue();
        isRemovedAgain.Should().BeFalse();
    }

    // Тест 4: Полная очистка кэша
    [Fact]
    public void ClearCache_WhenCacheHasItems_ClearsAllTemplates()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        var context = new { Data = "123" };

        engine.RenderCached("id_1", "Template 1 {{ Data }}", context);
        engine.RenderCached("id_2", "Template 2 {{ Data }}", context);

        // Act
        engine.ClearCache();

        bool hasId1 = engine.RemoveFromCache("id_1");
        bool hasId2 = engine.RemoveFromCache("id_2");

        // Assert
        hasId1.Should().BeFalse();
        hasId2.Should().BeFalse();
    }

    // Тест 5: Проверка валидации входных параметров
    [Fact]
    public void RenderCached_EmptyTemplateId_ThrowsArgumentException()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string invalidId = "";
        string template = "Hello {{ Name }}";
        var context = new { Name = "John" };

        // Act
        Action act = () => engine.RenderCached(invalidId, template, context);

        // Assert
        act.Should().Throw<ArgumentException>()
           .And.ParamName.Should().Be("id");
    }

    [Fact]
    public void RenderCached_TemplateChangedForSameId_InvalidatesCacheAndReturnsNewResult()
    {
        // Arrange
        var engine = new ScribanTemplateEngine();
        string templateId = "report_template";
        var context = new { User = "Анна" };

        string originalTemplate = "Отчет создан для: {{ User }}.";
        string modifiedTemplate = "Новая выгрузка пользователя {{ User }}.";

        // Act
        // 1. Первый рендеринг (Scriban парсит и кэширует исходный AST-дерево)
        string firstResult = engine.RenderCached(templateId, originalTemplate, context);

        // 2. Второй рендеринг под тем же ID, но с другим текстом (старый кэш перезаписывается)
        string secondResult = engine.RenderCached(templateId, modifiedTemplate, context);

        // Assert
        firstResult.Should().Be("Отчет создан для: Анна.");
        secondResult.Should().Be("Новая выгрузка пользователя Анна.");
    }

    [Fact]
    public void Render_RawHtmlInTemplateString_ReturnsHtmlAsIsWithoutEscaping()
    {
        // Arrange (Подготовка)
        var engine = new ScribanTemplateEngine();

        // HTML-теги написаны прямо в тексте шаблона, а не передаются через контекст
        string template = "<div class=\"container\"><ul><li>Привет, {{Name}}!</li></ul></div>";
        var context = new { Name = "Иван" };

        // Act (Действие)
        string result = engine.Render(template, context);

        // Assert (Проверка)
        // Проверяем, что весь сырой HTML-код остался нетронутым и не экранировался
        result.Should().Be("<div class=\"container\"><ul><li>Привет, Иван!</li></ul></div>");
    }
}
