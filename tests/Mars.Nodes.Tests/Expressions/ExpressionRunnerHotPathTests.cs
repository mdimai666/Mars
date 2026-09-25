using FluentAssertions;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Expressions;
using NSubstitute;

namespace Mars.Nodes.Tests.Expressions;

public class ExpressionRunnerHotPathTests
{
    static readonly IRuntimeNodeScope Rns = Substitute.For<IRuntimeNodeScope>();
    static readonly InjectNode Node = new();

    static NodeMsg Msg()
    {
        var msg = new NodeMsg { Payload = "hello" };
        msg.Set("topic", "sensor/1");
        msg.Set("count", 42);
        return msg;
    }

    static object? Resolve(string kind, string value, string varType, NodeMsg? msg = null)
    {
        msg ??= Msg();
        return new ExpressionRunner().Resolve(kind, value, varType, new ExpressionScope(Rns, msg), Node, "test");
    }

    [Fact]
    public void HotPath_MsgPayload_ReturnsPayload()
    {
        //Arrange
        //Act
        var result = Resolve(InputValueKind.Expression, "msg.Payload", "");

        //Assert
        result.Should().Be("hello");
    }

    [Fact]
    public void HotPath_MsgKind_ResolvesSameAsExpression()
    {
        //Arrange
        //Act
        var result = Resolve(InputValueKind.Msg, "Payload", "");

        //Assert
        result.Should().Be("hello");
    }

    [Fact]
    public void HotPath_ContextKey_Resolves()
    {
        //Arrange
        //Act
        var result = Resolve(InputValueKind.Expression, "msg.topic", "");

        //Assert
        result.Should().Be("sensor/1");
    }

    [Fact]
    public void HotPath_WithVarTypeConversion_Converts()
    {
        //Arrange
        //Act
        var asString = Resolve(InputValueKind.Expression, "msg.count", "string");
        var asInt = Resolve(InputValueKind.Msg, "count", "int");

        //Assert
        asString.Should().Be("42");
        asInt.Should().Be(42);
    }

    [Fact]
    public void HotPath_NullPayload_ReturnsNull()
    {
        //Arrange
        var msg = new NodeMsg { Payload = null };

        //Act
        var result = Resolve(InputValueKind.Expression, "msg.Payload", "", msg);

        //Assert
        result.Should().BeNull();
    }

    [Fact]
    public void HotPath_NullPayloadWithStringVarType_ReturnsEmptyLikeEnginePath()
    {
        //Arrange
        var msg = new NodeMsg { Payload = null };

        //Act
        var result = Resolve(InputValueKind.Expression, "msg.Payload", "string", msg);

        //Assert
        result.Should().Be(""); // ConvertToVarType: string + null → "" (как и в DE-пути)
    }

    [Fact]
    public void HotPath_NullPayloadWithIntVarType_ThrowsLikeEnginePath()
    {
        //Arrange
        var msg = new NodeMsg { Payload = null };

        //Act
        var act = () => Resolve(InputValueKind.Expression, "msg.Payload", "int", msg);

        //Assert
        act.Should().Throw<NodeExecuteException>();
    }

    [Fact]
    public void HotPath_MissingKey_FallsToEngineAndThrows()
    {
        //Arrange
        //Act
        var act = () => Resolve(InputValueKind.Expression, "msg.missing", "");

        //Assert
        act.Should().Throw<NodeExecuteException>();
    }

    [Fact]
    public void HotPath_ComplexExpression_GoesThroughEngine()
    {
        //Arrange
        //Act
        var concat = Resolve(InputValueKind.Expression, "msg.Payload + \"!\"", "");
        var linq = Resolve(InputValueKind.Expression, "msg.Payload.Count() + 1", "");

        //Assert
        concat.Should().Be("hello!");
        linq.Should().Be(6);
    }

    [Theory]
    [InlineData("msg.Payload", true)]
    [InlineData("msg.a.b.c", true)]
    [InlineData("GlobalContext.key", true)]
    [InlineData("FlowContext.key", true)]
    [InlineData("VarNode.v1", true)]
    [InlineData("msg.Payload.Count() + 1", false)]
    [InlineData("msg.items[0]", false)]
    [InlineData("msg.Payload + \"!\"", false)]
    [InlineData("2 + 3", false)]
    [InlineData("Payload", false)]
    [InlineData("msg.", false)]
    [InlineData("", false)]
    public void IsSingleRootPath_Classifies(string path, bool expected)
    {
        //Arrange
        //Act
        var actual = InputValueResolver.IsSingleRootPath(path);

        //Assert
        actual.Should().Be(expected);
    }
}
