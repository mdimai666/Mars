using FluentAssertions;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Expressions;
using NSubstitute;

namespace Mars.Nodes.Tests.Expressions;

public class ExpressionRunnerLambdaCacheTests
{
    static readonly IRuntimeNodeScope Rns = Substitute.For<IRuntimeNodeScope>();
    static readonly InjectNode Node = new();

    static NodeMsg Msg(string payload = "hello")
    {
        var msg = new NodeMsg { Payload = payload };
        msg.Set("topic", "sensor/1");
        msg.Set("count", 42);
        return msg;
    }

    [Theory]
    [InlineData("msg.Payload.Count() + 1", 6)]
    [InlineData("\"x\" + msg.topic", "xsensor/1")]
    [InlineData("msg.Payload.ToUpper()", "HELLO")]
    [InlineData("msg.Payload.Split('l').Length", 3)]
    [InlineData("msg.count > 40 ? \"big\" : \"small\"", "big")]
    [InlineData("string.IsNullOrEmpty(msg.Payload)", false)]
    [InlineData("msg.Payload.Count() + msg.count", 47)]
    public void Resolve_ComplexExpression_ParityWithOldPath(string expression, object expected)
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
    public void Resolve_CachedLambda_TakesFreshValuesPerMessage()
    {
        //Arrange
        var runner = new ExpressionRunner();

        //Act — одна и та же кэшированная Lambda, разные сообщения
        var first = runner.Resolve(InputValueKind.Expression, "msg.Payload.Count() + 1", "", new ExpressionScope(Rns, Msg("hello")), Node, "test");
        var second = runner.Resolve(InputValueKind.Expression, "msg.Payload.Count() + 1", "", new ExpressionScope(Rns, Msg("abcd")), Node, "test");

        //Assert
        first.Should().Be(6);
        second.Should().Be(5);
    }

    [Fact]
    public void Resolve_ValueTypeChangeBetweenMessages_FreshResult()
    {
        //Arrange
        var runner = new ExpressionRunner();
        var msgInt = new NodeMsg { Payload = 42 };
        var msgString = new NodeMsg { Payload = "7" };

        //Act — простое выражение (NCalc): тип значения пути сменился между сообщениями
        var first = runner.Resolve(InputValueKind.Expression, "msg.Payload + 1", "", new ExpressionScope(Rns, msgInt), Node, "test");
        var second = runner.Resolve(InputValueKind.Expression, "msg.Payload + 1", "", new ExpressionScope(Rns, msgString), Node, "test");

        //Assert
        first.Should().Be(43);
        second.Should().Be("71");
    }

    [Fact]
    public void Resolve_ResidualMsgReference_FallsToFullPathAndThrows()
    {
        //Arrange — неразрешимый путь остаётся в тексте: кэш Lambda неприменим (msg captur'ится константой),
        //выражение идёт полным путём и падает канонической ошибкой DE
        var scope = new ExpressionScope(Rns, Msg());

        //Act
        var act = () => new ExpressionRunner().Resolve(InputValueKind.Expression, "msg.missing + 1", "", scope, Node, "test");

        //Assert
        act.Should().Throw<NodeExecuteException>();
    }

    [Fact]
    public void Resolve_DivisionByZero_ThrowsLikeOldPath()
    {
        //Arrange
        var scope = new ExpressionScope(Rns, Msg());

        //Act — NCalc-обработчик бросает DivideByZero → Reject → DE тоже бросает → NodeExecuteException
        var act = () => new ExpressionRunner().Resolve(InputValueKind.Expression, "msg.count / 0", "", scope, Node, "test");

        //Assert
        act.Should().Throw<NodeExecuteException>();
    }

    [Fact]
    public void Resolve_ParseFailure_FallsToFullPathErrors()
    {
        //Arrange
        var scope = new ExpressionScope(Rns, Msg());

        //Act — невалидный C# после подмены путей: Parse падает → полный путь даёт каноническую ошибку
        var act = () => new ExpressionRunner().Resolve(InputValueKind.Expression, "msg.Payload.Count() +", "", scope, Node, "test");

        //Assert
        act.Should().Throw<NodeExecuteException>();
    }
}
