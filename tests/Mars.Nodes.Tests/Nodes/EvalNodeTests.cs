using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Tests.Services;

namespace Mars.Nodes.Tests.Nodes;

public class EvalNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_ExpressionInput_EvaluatesToPayload()
    {
        //Arrange
        var node = new EvalNode { Input = "msg.Payload + 1" };

        //Act
        var msg = await ExecuteNode(node, new NodeMsg { Payload = 41 });

        //Assert
        msg.Payload.Should().Be(42);
    }

    [Fact]
    public async Task Execute_ConstInput_PutsLiteralToPayload()
    {
        //Arrange
        var node = new EvalNode { ValueKind = InputValueKind.Const, Input = "just text" };

        //Act
        var msg = await ExecuteNode(node, new NodeMsg());

        //Assert
        msg.Payload.Should().Be("just text");
    }
}
