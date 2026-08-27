using FluentAssertions;
using Mars.TemplateEngine.Providers.HandlebarsProvider;

namespace Test.Mars.Host.TemplateEngines.HandlebarsTests;

public class HandlebarsTemplateEngineTests : ITemplateEngineInterfaceTests
{
    // Тест 1: Обычный рендеринг (Без кэша)
    [Fact]
    public void Render_WithValidTemplateAndContext_ReturnsExpectedString()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();
        string template = "Привет, {{Name}}! Твой баланс: {{Balance}} руб.";
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
        var engine = new HandlebarsTemplateEngine();
        string templateId = "user_welcome";
        string template = "Welcome, {{User}}!";
        var context = new { User = "Alice" };

        // Act
        // Первый вызов (компиляция и сохранение в кэш)
        string firstResult = engine.RenderCached(templateId, template, context);

        // Повторный вызов (извлечение из кэша)
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
        var engine = new HandlebarsTemplateEngine();
        string templateId = "temporary_template";
        string template = "Value: {{Value}}";
        var context = new { Value = "Test" };

        // Добавляем в кэш
        engine.RenderCached(templateId, template, context);

        // Act
        bool isRemoved = engine.RemoveFromCache(templateId);

        // Пытаемся удалить его еще раз, чтобы проверить, что его там больше нет
        bool isRemovedAgain = engine.RemoveFromCache(templateId);

        // Assert
        isRemoved.Should().BeTrue("потому что шаблон существовал в кэше");
        isRemovedAgain.Should().BeFalse("потому что шаблон уже был удален ранее");
    }

    // Тест 4: Полная очистка кэша
    [Fact]
    public void ClearCache_WhenCacheHasItems_ClearsAllTemplates()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();
        var context = new { Data = "123" };

        // Наполняем кэш несколькими шаблонами
        engine.RenderCached("id_1", "Template 1 {{Data}}", context);
        engine.RenderCached("id_2", "Template 2 {{Data}}", context);

        // Act
        engine.ClearCache();

        // Проверяем, что элементы удалены (метод Remove должен вернуть false для очищенного кэша)
        bool hasId1 = engine.RemoveFromCache("id_1");
        bool hasId2 = engine.RemoveFromCache("id_2");

        // Assert
        hasId1.Should().BeFalse("так как кэш был полностью очищен");
        hasId2.Should().BeFalse("так как кэш был полностью очищен");
    }

    // Тест 5: Проверка валидации входных параметров (Негативный сценарий)
    [Fact]
    public void RenderCached_EmptyTemplateId_ThrowsArgumentException()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();
        string invalidId = "   "; // Пробелы вместо нормального ID
        string template = "Hello {{Name}}";
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
        var engine = new HandlebarsTemplateEngine();
        string templateId = "dynamic_mail";
        var context = new { Name = "Иван" };

        string oldTemplate = "Привет, {{Name}}!";
        string newTemplate = "Здравствуйте, {{Name}}.";

        // Act
        // 1. Первый вызов со старым шаблоном
        string firstResult = engine.RenderCached(templateId, oldTemplate, context);

        // 2. Второй вызов под ТЕМ ЖЕ ID, но с измененным текстом шаблона
        string secondResult = engine.RenderCached(templateId, newTemplate, context);

        // Assert
        firstResult.Should().Be("Привет, Иван!");
        secondResult.Should().Be("Здравствуйте, Иван."); // Проверяем, что кэш инвалидировался
    }

    [Fact]
    public void Render_RawHtmlInTemplateString_ReturnsHtmlAsIsWithoutEscaping()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();

        // HTML-теги написаны прямо в тексте шаблона, а не передаются через контекст
        string template = "<div class=\"container\"><ul><li>Привет, {{Name}}!</li></ul></div>";
        var context = new { Name = "Иван" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        // Проверяем, что весь сырой HTML-код остался нетронутым и не экранировался
        result.Should().Be("<div class=\"container\"><ul><li>Привет, Иван!</li></ul></div>");
    }

    [Fact]
    public void Render_TripleBraces_ReturnsUnescapedHtml()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();

        // Тройные скобки {{{ }}} отключают HTML-экранирование
        string template = "<div>{{{HtmlContent}}}</div>";
        var context = new { HtmlContent = "<strong>Привет, мир!</strong>" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        // Проверяем, что теги <strong> остались в исходном виде
        result.Should().Be("<div><strong>Привет, мир!</strong></div>");
    }

    [Fact]
    public void Render_DoubleBraces_ReturnsEscapedHtmlText()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();

        // Двойные скобки {{ }} принудительно экранируют спецсимволы HTML
        string template = "<div>{{HtmlContent}}</div>";
        var context = new { HtmlContent = "<strong>Привет, мир!</strong>" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        // Проверяем, что символы < и > превратились в безопасные HTML-сущности
        result.Should().Be("<div>&lt;strong&gt;Привет, мир!&lt;/strong&gt;</div>");
    }

    [Fact]
    public void Render_CyrillicAndHtml_EscapesHtmlButKeepsCyrillicIntact()
    {
        // Arrange
        var engine = new HandlebarsTemplateEngine();

        string template = "Текст: {{Value}}";
        // Передаем переменную, содержащую и кириллицу, и HTML-тег
        var context = new { Value = "Привет, <b>мир</b>!" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        // Ожидаем: Кириллические буквы остались буквами, а теги <b> и </b> заэкранировались
        result.Should().Be("Текст: Привет, &lt;b&gt;мир&lt;/b&gt;!");
    }
}
