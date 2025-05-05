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

        var node = new SwitchNode { Conditions = [
            new SwitchNode.Condition{ Key = "contdition1", Value = "Payload == 123" },
            new SwitchNode.Condition{ Key = "contdition2", Value = "Payload != 123" },
        ]};

        //Act
        var msg1 = await ExecuteNodeEx(node, input1);
        var msg2 = await ExecuteNodeEx(node, input2);

        //Assert
        msg1.OutputPort.Should().Be(0);
        msg1.Msg.Should().Be(input1);
        msg2.OutputPort.Should().Be(1);
        msg2.Msg.Should().Be(input2);
    }
}
