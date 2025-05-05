using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Implements.Test.Services;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Nodes;

public class FunctionNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_SimpleEval_ShouldSuccess()
    {
        //Arrange
        var code = "return 1+1;";

        //Act
        var msg = await ExecuteFunctionNode(code);

        //Assert
        msg.Payload.Should().Be(2);
    }

    [Fact]
    public async Task FlowContextVaribles_SetValueFromFunctionNode_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FunctionNodeImpl.Execute);
        _ = nameof(FunctionNodeImpl.ScriptExecuteContext.Flow);
        _ = nameof(FlowNodeImpl.Context);
        var code = """
            int v = 123;
            Flow.Context.SetValue("v", v);
            return v;
            """;

        //Act
        var (flowNode, _, msg) = await ExecuteFunctionNodeEx(code);

        //Assert
        flowNode.Context.GetValue<int>("v").Should().Be(123);
        msg.Payload.Should().Be(123);
    }

    [Fact]
    public async Task GlobalContextVaribles_SetValueFromFunctionNode_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FunctionNodeImpl.Execute);
        _ = nameof(FunctionNodeImpl.ScriptExecuteContext.GlobalContext);
        _ = nameof(IRED.GlobalContext);
        var code = """
            int v = 123;
            GlobalContext.SetValue("v", v);
            return v;
            """;

        //Act
        var msg = await ExecuteFunctionNode(code);

        //Assert
        RED.GlobalContext.GetValue<int>("v").Should().Be(123);
        msg.Payload.Should().Be(123);
    }
}
