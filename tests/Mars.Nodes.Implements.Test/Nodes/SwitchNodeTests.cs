using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Implements.Test.Services;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Nodes;

public class SwitchNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_Switch_Success()
    {
        //Arrange
        _ = nameof(SwitchNodeImpl.Execute);
        var input1 = new NodeMsg() { Payload = 123 };
        var input2 = new NodeMsg() { Payload = 333 };

        var node = new SwitchNode
        {
            Conditions = [
            new SwitchNode.Condition{ Key = "contdition1", Value = "Payload == 123" },
            new SwitchNode.Condition{ Key = "contdition2", Value = "Payload != 123" },
        ]
        };

        //Act
        var msg1 = await ExecuteNodeEx(node, input1);
        var msg2 = await ExecuteNodeEx(node, input2);

        //Assert
        msg1.OutputPort.Should().Be(0);
        msg1.Msg.Should().Be(input1);
        msg2.OutputPort.Should().Be(1);
        msg2.Msg.Should().Be(input2);
    }

    [Theory]
    [InlineData("Payload == 123", 123, true)]
    [InlineData("Payload > 10", 5, false)]
    [InlineData("Payload == \"123\"", "123", true)]
    public async Task Execute_ExpressionsWorkingTest_Success(string condition, object payload, bool expect)
    {
        //Arrange
        _ = nameof(SwitchNodeImpl.Execute);
        var input = new NodeMsg() { Payload = payload };

        var node = new SwitchNode
        {
            BreakAfterFirst = true,
            Conditions = [
                new SwitchNode.Condition{ Key = "contdition1", Value = condition },
                new SwitchNode.Condition{ Key = "contdition2", Value = "true" },
            ]
        };

        //Act
        var msg = await ExecuteNodeEx(node, input);

        //Assert
        msg.OutputPort.Should().Be(expect ? 0 : 1);
    }

    [Fact]
    public async Task Execute_ObjectPropertyAccess_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SwitchNodeImpl.Execute);
        var input = new NodeMsg() { Payload = 123 };
        var extraObject = new SwitchNode.Condition { Key = "key1", Value = "valueIsWork!" };
        input.Add(extraObject);

        var node = new SwitchNode
        {
            Conditions = [
                new SwitchNode.Condition{ Key = "contdition1", Value = "Condition.Value == \"valueIsWork!\"" },
                new SwitchNode.Condition{ Key = "contdition2", Value = "true" },
            ]
        };

        //Act
        var msg = await ExecuteNodeEx(node, input);

        //Assert
        input.AsFullDict().Should().ContainKey("Condition");
        msg.OutputPort.Should().Be(0);
        msg.Msg.Should().Be(input);
    }
}
