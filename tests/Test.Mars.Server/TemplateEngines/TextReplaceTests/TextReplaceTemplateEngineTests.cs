using FluentAssertions;
using Mars.TemplateEngine.Host.InternalProviders;

namespace Test.Mars.Host.TemplateEngines.TextReplaceTests;

public class TextReplaceTemplateEngineTests
{
    // Тест 1: Обычный рендеринг (Без кэша)
    [Fact]
    public void Render_WithValidTemplateAndContext_ReturnsExpectedString()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string template = "Баланс: {Balance}$";
        var context = new { Balance = 100 };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Баланс: 100$");
    }

    // Тест 2: Рендеринг с использованием кэша (Первый и повторный вызов)
    [Fact]
    public void RenderCached_CalledMultipleTimes_ReturnsCorrectResult()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string templateId = "regex_email_template";
        string template = "Hello, {User}!";
        var context = new { User = "Bob" };

        // Act
        string firstResult = engine.RenderCached(templateId, template, context);
        string secondResult = engine.RenderCached(templateId, template, context);

        // Assert
        firstResult.Should().Be("Hello, Bob!");
        secondResult.Should().Be("Hello, Bob!");
    }

    // Тест 3: Удаление конкретного скомпилированного Regex из кэша
    [Fact]
    public void RemoveFromCache_ExistingId_RemovesTemplateAndReturnsTrue()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string templateId = "temp_regex";
        string template = "Data: {Data}";
        var context = new { Data = "Info" };

        engine.RenderCached(templateId, template, context);

        // Act
        bool isRemoved = engine.RemoveFromCache(templateId);
        bool isRemovedAgain = engine.RemoveFromCache(templateId);

        // Assert
        isRemoved.Should().BeTrue();
        isRemovedAgain.Should().BeFalse();
    }

    // Тест 4: Полная очистка кэша Regex
    [Fact]
    public void ClearCache_WhenCacheHasItems_ClearsAllCompiledRegex()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        var context = new { Text = "Ok" };

        engine.RenderCached("r_1", "1: {Text}", context);
        engine.RenderCached("r_2", "2: {Text}", context);

        // Act
        engine.ClearCache();

        bool hasId1 = engine.RemoveFromCache("r_1");
        bool hasId2 = engine.RemoveFromCache("r_2");

        // Assert
        hasId1.Should().BeFalse();
        hasId2.Should().BeFalse();
    }

    // Тест 5: Проверка валидации входных параметров (Негативный сценарий)
    [Fact]
    public void RenderCached_EmptyTemplateId_ThrowsArgumentException()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string invalidId = null!;
        string template = "Hi {User}";
        var context = new { User = "Max" };

        // Act
        Action act = () => engine.RenderCached(invalidId, template, context);

        // Assert
        act.Should().Throw<ArgumentException>()
           .And.ParamName.Should().Be("id");
    }

    [Fact]
    public void RenderCached_WhenTemplateChanges_OverwritesOldRegexCacheAndReturnsNewResult()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string templateId = "user_alert";
        var context = new { Task = "Бэкап" };

        string originalTemplate = "Внимание! Запущен {Task}.";
        string modifiedTemplate = "Уведомление: Процесс {Task} завершен.";

        // Act
        // 1. Рендерим первый вариант (Regex компилируется и сохраняется)
        string firstResult = engine.RenderCached(templateId, originalTemplate, context);

        // 2. Рендерим измененный вариант под тем же ID (старый Regex удаляется, компилируется новый)
        string secondResult = engine.RenderCached(templateId, modifiedTemplate, context);

        // Assert
        firstResult.Should().Be("Внимание! Запущен Бэкап.");
        secondResult.Should().Be("Уведомление: Процесс Бэкап завершен.");
    }
}
