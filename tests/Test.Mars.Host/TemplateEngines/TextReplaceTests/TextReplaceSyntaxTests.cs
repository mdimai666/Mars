using FluentAssertions;
using Mars.TemplateEngine.Host.InternalProviders;

namespace Test.Mars.Host.TemplateEngines.TextReplaceTests;

public class TextReplaceSyntaxTests
{
    // 1. Тест: Базовый вывод одной и нескольких переменных
    [Fact]
    public void Render_VariableSubstitution_ReplacesPlaceholdersWithValues()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string template = "Пользователь {User} имеет статус {Value}.";
        var context = new { User = "Иван", Value = "Активен" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Пользователь Иван имеет статус Активен.");
    }

    // 2. Тест: Корректное экранирование двойными скобками {{Property}}
    [Fact]
    public void Render_EscapedProperty_ReturnsSingleBracesWithoutReplacingValue()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string template = "Это обычная {Value}, а это экранированная {{Value}}.";
        var context = new { Value = "Тест" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Это обычная Тест, а это экранированная {Value}.");
    }

    // 3. Тест: Регистронезависимость при поиске свойств (OrdinalIgnoreCase)
    [Fact]
    public void Render_DifferentCaseInTemplate_StillFindsAndReplacesProperty()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string template = "Привет, {user}! Твой ID: {id}."; // В шаблоне lower_case
        var context = new { User = "Ольга", Id = 777 };       // В коде PascalCase

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Привет, Ольга! Твой ID: 777.");
    }

    // 4. Тест: Поведение, если свойства НЕТ в контексте (Оставляем {Property} как есть)
    [Fact]
    public void Render_MissingPropertyInContext_LeavesPlaceholderUntouched()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string template = "Существующее: {Exist}, Отсутствующее: {Missing}.";
        var context = new { Exist = "Да" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Существующее: Да, Отсутствующее: {Missing}.");
    }

    // 5. Тест: Передача Dictionary<string, string> вместо анонимного класса
    [Fact]
    public void Render_DictionaryContext_CorrectlyReplacesValues()
    {
        // Arrange
        var engine = new TextReplaceTemplateEngine();
        string template = "Ключ: {Key1}, Значение: {Key2}";
        var context = new Dictionary<string, string>
        {
            { "Key1", "Имя" },
            { "Key2", "Алексей" }
        };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Ключ: Имя, Значение: Алексей");
    }
}
