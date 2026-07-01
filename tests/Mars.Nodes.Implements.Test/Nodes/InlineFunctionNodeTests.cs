using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Implements.Test.Services;

namespace Mars.Nodes.Implements.Test.Nodes;

public class InlineFunctionNodeTests : NodeServiceUnitTestBase
{

    InlineFunctionNode CreateInlineFunction(string typeId, Delegate @delegate, string[]? args = null)
    {
        var def = new InlineFunctionNodeDefinition()
        {
            //TypeId = "core.inlineFunctions.RandomNumber",
            TypeId = typeId,
            Name = "Name." + typeId,
            Inputs = [new()],
            Outputs = [new()],
            Color = "#40b39b",
            Icon = "",
            GroupName = "function",
            Delegate = @delegate
        };
        _nodeImplementFactory.RegisterInlineFunctionNode(def);
        return _nodeImplementFactory.CreateInlineFunctionNode(def, args ?? []);
    }

    [Fact]
    public async Task Execute_DelegateWithoutArgument_ShouldSuccess()
    {
        //Arrange
        _ = nameof(DebugNodeImpl.Execute);
        var input = new NodeMsg() { Payload = 0 };
        var node = CreateInlineFunction("TwoPlusTwo", () => 1 + 1);

        //Act
        var msg = await RunUsingTaskManager(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo(2);
    }

    [Fact]
    public async Task Execute_DelegateWithMsgAndExecuteParams_ShouldSuccess()
    {
        //Arrange
        _ = nameof(DebugNodeImpl.Execute);
        var input = new NodeMsg() { Payload = 123 };
        var node = CreateInlineFunction("Paramtered", (NodeMsg msg, ExecutionParameters @params) => msg != null && @params != null && (int)msg.Payload! == 123);

        //Act
        var msg = await RunUsingTaskManager(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo(true);
    }

    [Fact]
    public async Task Execute_AsyncDelegateWithParams_ShouldSuccess()
    {
        //Arrange
        _ = nameof(DebugNodeImpl.Execute);
        var input = new NodeMsg() { Payload = 123 };
        var node = CreateInlineFunction("Paramtered", async (NodeMsg msg, ExecutionParameters @params) =>
        {
            await Task.Delay(100);
            return 123;
        });

        //Act
        var msg = await RunUsingTaskManager(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo(123);
    }

    [Fact]
    public async Task Execute_DelegatePassArguments_ShouldSuccess()
    {
        //Arrange
        _ = nameof(DebugNodeImpl.Execute);
        var input = new NodeMsg() { Payload = 44 };
        var node = CreateInlineFunction("Multiply", (NodeMsg msg, int multiplier) => (int)msg.Payload! * multiplier, args: ["2"]);

        //Act
        var msg = await RunUsingTaskManager(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo(88);
    }

}
