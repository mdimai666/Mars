using FluentAssertions;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Nodes.Tests.Expressions;

public class ExpressionRunnerPoolTests
{
    [Fact]
    public void Rent_AfterReturn_ReusesSameRunner()
    {
        //Arrange
        var pool = new ExpressionRunnerPool();

        //Act
        var first = pool.Rent("node-1");
        pool.Return("node-1", first);
        var second = pool.Rent("node-1");

        //Assert
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void Rent_WhileRented_GivesDifferentRunner()
    {
        //Arrange
        var pool = new ExpressionRunnerPool();

        //Act
        var first = pool.Rent("node-1");
        var second = pool.Rent("node-1");

        //Assert
        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public void Rent_DifferentNodes_Isolated()
    {
        //Arrange
        var pool = new ExpressionRunnerPool();

        //Act
        var first = pool.Rent("node-1");
        pool.Return("node-1", first);
        var other = pool.Rent("node-2");

        //Assert
        other.Should().NotBeSameAs(first);
    }

    [Fact]
    public void Runner_ReusedInterpreter_RebindsMsgPerMessage()
    {
        //Arrange
        var pool = new ExpressionRunnerPool();
        var rns = Substitute.For<IRuntimeNodeScope>();
        var node = new InjectNode();
        var runner = pool.Rent(node.Id);
        var msg1 = new NodeMsg { Payload = "one" };
        var msg2 = new NodeMsg { Payload = null };

        //Act
        var first = runner.Resolve(InputValueKind.Expression, "msg.Payload", "", new ExpressionScope(rns, msg1), node, "test");
        var second = runner.Resolve(InputValueKind.Expression, "msg.Payload", "", new ExpressionScope(rns, msg2), node, "test");

        //Assert
        first.Should().Be("one");
        second.Should().BeNull(); // stale-обёртка от msg1 вернула бы "one"
    }

    [Fact]
    public void Session_WithoutPool_ResolvesAsEphemeral()
    {
        //Arrange
        var rns = Substitute.For<IRuntimeNodeScope>();
        var node = new InjectNode();
        var msg = new NodeMsg { Payload = "hello" };

        //Act
        using var session = rns.Expressions(node);
        var constant = session.Resolve(InputValueKind.Const, "42", "int", msg, node, "test");
        var expression = session.Resolve(InputValueKind.Expression, "msg.Payload + \"!\"", "", msg, node, "test");

        //Assert
        constant.Should().Be(42);
        expression.Should().Be("hello!");
    }

    [Fact]
    public void Session_WithPool_ResolvesAcrossSessions()
    {
        //Arrange
        var pool = new ExpressionRunnerPool();
        var rns = Substitute.For<IRuntimeNodeScope>();
        rns.ServiceProvider.Returns(new ServiceCollection().AddSingleton(pool).BuildServiceProvider());
        var node = new InjectNode();
        var msg = new NodeMsg { Payload = "hello" };

        //Act
        using var first = rns.Expressions(node);
        var firstResult = first.Resolve(InputValueKind.Expression, "msg.Payload", "", msg, node, "test");
        first.Dispose();

        using var second = rns.Expressions(node);
        var secondResult = second.Resolve(InputValueKind.Expression, "msg.Payload + \"!\"", "", msg, node, "test");

        //Assert
        firstResult.Should().Be("hello");
        secondResult.Should().Be("hello!"); // вторая сессия переиспользует возвращённый runner
    }
}
