using FluentAssertions;
using Mars.TemplateEngine.Providers.ScribanProvider;

namespace Test.Mars.Host.TemplateEngines.ScribanTests;

public class ScribanRazorStyleSyntaxTests
{
    // 1. Тест: Вывод переменных через привычный @
    [Fact]
    public void Render_VariableSubstitution_ReplacesPlaceholdersWithValues()
    {
        // Arrange
        var engine = new ScribanRazorStyleTemplateEngine();
        string template = "Пользователь @User.Name, статус: @Status";
        var context = new
        {
            User = new { Name = "Игорь" },
            Status = "Активен"
        };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Пользователь Игорь, статус: Активен");
    }

    // 2. Тест: Условие @if (...) { ... } else { ... } с закрытием через простую скобку }
    [Fact]
    public void Render_IfElseCondition_RendersCorrectBlockBasedOnContext()
    {
        // Arrange
        var engine = new ScribanRazorStyleTemplateEngine();
        string template = "Доступ: @if(IsAdmin) { <p>Админ</p> } else { <p>Юзер</p> }";

        var contextTrue = new { IsAdmin = true };
        var contextFalse = new { IsAdmin = false };

        // Act
        string resultTrue = engine.Render(template, contextTrue);
        string resultFalse = engine.Render(template, contextFalse);

        // Assert
        resultTrue.Should().Be("Доступ:  <p>Админ</p> ");
        resultFalse.Should().Be("Доступ:  <p>Юзер</p> ");
    }

    [Fact]
    public void Render_IfElseCondition_RendersCorrectBlockBasedOnContextSubfield()
    {
        // Arrange
        var engine = new ScribanRazorStyleTemplateEngine();
        string template = "Доступ: @if( User.IsAdmin ) { <p>Админ</p> } else { <p>Юзер</p> }";

        var contextTrue = new { User = new { IsAdmin = true } };
        var contextFalse = new { User = new { IsAdmin = false } };

        // Act
        string resultTrue = engine.Render(template, contextTrue);
        string resultFalse = engine.Render(template, contextFalse);

        // Assert
        resultTrue.Should().Be("Доступ:  <p>Админ</p> ");
        resultFalse.Should().Be("Доступ:  <p>Юзер</p> ");
    }

    // 3. Тест: Сложное условие со слотом else if
    [Fact]
    public void Render_ElseIfCondition_RendersCorrectBranch()
    {
        // Arrange
        var engine = new ScribanRazorStyleTemplateEngine();
        string template = "@if(Role == \"Admin\") { А } else if(Role == \"Manager\") { М } else { Ю }";
        var context = new { Role = "Manager" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be(" М ");
    }

    // 4. Тест: Отсутствующее свойство возвращает пустоту (поведение Scriban)
    [Fact]
    public void Render_MissingProperty_OutputsEmptyString()
    {
        // Arrange
        var engine = new ScribanRazorStyleTemplateEngine();
        string template = "Значение: @NotExistsProperty";
        var context = new { Existing = "Да" };

        // Act
        string result = engine.Render(template, context);

        // Assert
        result.Should().Be("Значение: ");
    }
}
