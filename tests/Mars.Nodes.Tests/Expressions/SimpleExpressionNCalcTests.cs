using FluentAssertions;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Expressions;
using NSubstitute;

namespace Mars.Nodes.Tests.Expressions;

public class SimpleExpressionNCalcTests
{
    static readonly IRuntimeNodeScope Rns = Substitute.For<IRuntimeNodeScope>();
    static readonly InjectNode Node = new();

    static NodeMsg Msg()
    {
        var msg = new NodeMsg { Payload = "hello" };
        msg.Set("topic", "sensor/1");
        msg.Set("count", 42);
        msg.Set("flag", true);
        msg.Set("ts", 1758800000000L);
        return msg;
    }

    [Theory]
    [InlineData("msg.count * 2 + 1")]
    [InlineData("2 + 3")]
    [InlineData("msg.Payload == \"hello\"")]
    [InlineData("msg.count > 40 && msg.flag")]
    [InlineData("msg.count > 50 || !msg.flag")]
    [InlineData("(1 + 2) * 3")]
    [InlineData("10 % 3")]
    [InlineData("1.5 + 2")]
    [InlineData("true")]
    [InlineData("GlobalContext.key + FlowContext.other")]
    [InlineData("msg.Payload.Length > 3")] // много-сегментный путь: оба движка резолвят одним TryResolveRootPath
    [InlineData("2 / 4")]                  // деление — C#-семантика через обработчик CSharpIntegerDivision
    [InlineData("msg.count / 4 * 4")]
    public void Classifier_AcceptsSimple(string expression)
    {
        //Arrange
        //Act
        var simple = SimpleExpressionClassifier.IsSimple(expression);

        //Assert
        simple.Should().BeTrue();
    }

    [Theory]
    [InlineData("msg.Payload.Count() + 1")]                  // вызов/LINQ
    [InlineData("msg.Payload.Substring(0, 2)")]
    [InlineData("string.IsNullOrEmpty(msg.Payload)")]        // статика
    [InlineData("msg.count > 10 ? \"big\" : \"small\"")]     // тернарник — не пускаем в v1
    [InlineData("msg.count > 40 and msg.flag")]              // NCalc-only синтаксис
    [InlineData("x + 1")]                                    // голый идентификатор
    [InlineData("Payload")]
    [InlineData("")]
    public void Classifier_RejectsNonSimple(string expression)
    {
        //Arrange
        //Act
        var simple = SimpleExpressionClassifier.IsSimple(expression);

        //Assert
        simple.Should().BeFalse();
    }

    [Fact]
    public void Classifier_ConvertsRootPaths_KeepsStringLiterals()
    {
        //Arrange
        //Act
        var converted = SimpleExpressionClassifier.TryGetForm("msg.count * 2 + msg.ts", out var form1);
        var literal = SimpleExpressionClassifier.TryGetForm("\"msg.Payload\" + msg.topic", out var form2);

        //Assert
        converted.Should().BeTrue();
        form1.Text.Should().Be("msg_count * 2 + msg_ts");
        form1.Paths.Should().Equal("msg.count", "msg.ts");
        form1.Params.Should().Equal("msg_count", "msg_ts");

        literal.Should().BeTrue();
        form2.Text.Should().Be("\"msg.Payload\" + msg_topic"); // путь внутри литерала не подменяется
        form2.Paths.Should().Equal("msg.topic");
    }

    [Theory]
    [InlineData("msg.count * 2 + 1", 85)]
    [InlineData("msg.count > 40", true)]
    [InlineData("msg.Payload + \"!\"", "hello!")]
    [InlineData("msg.Payload == \"hello\"", true)]
    [InlineData("!msg.flag", false)]
    [InlineData("msg.count > 50 || msg.flag", true)]
    [InlineData("(1 + 2) * 3", 9)]
    [InlineData("10 % 3", 1)]
    [InlineData("1.5 + 2", 3.5)]
    [InlineData("msg.ts + 1000", 1758800001000L)]
    [InlineData("2 / 4", 0)]                        // деление → NCalc + C#-обработчик (целочисленное)
    [InlineData("7 / 2", 3)]
    [InlineData("msg.count / 4", 10)]
    [InlineData("msg.count / 4 * 4", 40)]
    [InlineData("msg.ts / 1000", 1758800000L)]      // long/long → long
    [InlineData("7.0 / 2", 3.5)]                    // floating-point — дефолтный путь NCalc
    [InlineData("-7 % 3", -1)]                      // знак остатка как в C#
    [InlineData("msg.Payload.Count() + 1", 6)]      // member access → DE
    [InlineData("msg.Payload.Length > 3", true)]    // путь-свойство → NCalc, значение то же (общий TryResolveRootPath)
    public void Resolve_ParityWithDynamicExpresso(string expression, object expected)
    {
        //Arrange
        var msg = Msg();
        var scope = new ExpressionScope(Rns, msg);

        //Act
        var runnerResult = new ExpressionRunner().Resolve(InputValueKind.Expression, expression, "", scope, Node, "test");

        var interpreter = InputValueResolver.CreateInterpreter(Rns, msg);
        var deResult = InputValueResolver.ResolveExpression(expression, "", interpreter, scope, Node, "test");

        //Assert
        runnerResult.Should().Be(deResult);
        runnerResult.Should().Be(expected);
    }

    [Fact]
    public void Resolve_BoolCoercion_StrictTypeMatchingLikeDocumented()
    {
        //Arrange
        var scope = new ExpressionScope(Rns, Msg());

        //Act
        // задокументированное расхождение (SEMANTIC.md #21, решение 5 плана):
        // C#/DE бросил бы исключение, NCalc c StrictTypeMatching даёт false
        var result = new ExpressionRunner().Resolve(InputValueKind.Expression, "1 == true", "", scope, Node, "test");

        //Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Resolve_MissingPath_ThrowsLikeEnginePath()
    {
        //Arrange
        var scope = new ExpressionScope(Rns, Msg());

        //Act
        var act = () => new ExpressionRunner().Resolve(InputValueKind.Expression, "msg.missing == null", "", scope, Node, "test");

        //Assert
        // NCalc-путь отказывается (путь не разрешился) → DE → его штатная ошибка
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Resolve_NCalcFailure_FallsBackToDe()
    {
        //Arrange
        var runner = new ExpressionRunner();
        var scope = new ExpressionScope(Rns, Msg());

        //Act
        var first = runner.Resolve(InputValueKind.Expression, "msg.count * 3 + 1", "", scope, Node, "test");
        SimpleExpressionClassifier.Reject("msg.count * 3 + 1"); // имитация отказа NCalc
        var second = runner.Resolve(InputValueKind.Expression, "msg.count * 3 + 1", "", scope, Node, "test");

        //Assert
        first.Should().Be(127);
        second.Should().Be(127); // тот же результат через DE
    }
}
