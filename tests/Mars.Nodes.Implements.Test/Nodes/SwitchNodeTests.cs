using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Implements.Test.Services;

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
            new SwitchNode.Condition{ Value = "msg.Payload == 123" },
            new SwitchNode.Condition{ Value = "msg.Payload != 123" },
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
    [InlineData("msg.Payload == 123", 123, true)]
    [InlineData("msg.Payload > 10", 5, false)]
    [InlineData("msg.Payload == \"123\"", "123", true)]
    public async Task Execute_ExpressionsWorkingTest_Success(string condition, object payload, bool expect)
    {
        //Arrange
        _ = nameof(SwitchNodeImpl.Execute);
        var input = new NodeMsg() { Payload = payload };

        var node = new SwitchNode
        {
            BreakAfterFirst = true,
            Conditions = [
                new SwitchNode.Condition{ Value = condition },
                new SwitchNode.Condition{ Value = "true" },
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
        var extraObject = new SwitchNode.Condition { Value = "valueIsWork!" };
        input.Add(extraObject);

        var node = new SwitchNode
        {
            Conditions = [
                new SwitchNode.Condition{ Value = "msg.Condition.Value == \"valueIsWork!\"" },
                new SwitchNode.Condition{ Value = "true" },
            ]
        };

        //Act
        var msg = await ExecuteNodeEx(node, input);

        //Assert
        input.AsFullDict().Should().ContainKey("Condition");
        msg.OutputPort.Should().Be(0);
        msg.Msg.Should().Be(input);
    }

    [Fact]
    public async Task ChainAfterJsonNode_ObjectPropertyAccess_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SwitchNodeImpl.Execute);
        string json = """
                {
                    "name":"Dima",
                    "age":35
                }
                """;
        var input = new NodeMsg() { Payload = json };
        var node = new SwitchNode
        {
            BreakAfterFirst = true,
            Conditions = [
                new SwitchNode.Condition{ Value = "false" },
                new SwitchNode.Condition{ Value = "msg.Payload.age == 35" },
                new SwitchNode.Condition{ Value = "true" }, //else
            ]
        };

        //Act
        var result = await RunUsingTaskManagerEx(NodesWorkflowBuilder.Create()
                                                .AddNext(new JsonNode())
                                                .AddNext(node)
                                            , input);

        //Assert
        result.Should().NotBeNull();
        result.OutputPort.Should().Be(1);
        result.Msg.Should().Be(input);
    }

}
