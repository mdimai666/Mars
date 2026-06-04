using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Implements.Test.Services;

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
        msg.Payload.Should().BeOfType<int>();
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
        var (flowNode, msg) = await ExecuteFunctionNodeEx(code);

        //Assert
        flowNode.Context.GetValue<int>("v").Should().Be(123);
        msg.Payload.Should().BeOfType<int>();
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

    [Fact]
    public async Task Execute_AcceptJsonNodeOutput_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FunctionNodeImpl.Execute);
        _ = nameof(DynamicJson);
        _ = nameof(IRED.GlobalContext);
        var json = """
                {
                    "name":"Dima",
                    "age":35
                }
                """;
        var code = """
            return msg.Payload.age * 2;
            """;

        //Act
        var msg = await RunUsingTaskManager(NodesWorkflowBuilder.Create()
                                                .AddNext(new Core.Nodes.JsonNode())
                                                .AddNext(new FunctionNode { Code = code })
                                            , new Core.NodeMsg { Payload = json });

        //Assert
        msg.Payload.Should().Be(70);
    }

    [Fact]
    public async Task Execute_ChangeDictionaryField_ShouldSuccessReturnCorrentDictionaryType()
    {
        //Arrange
        _ = nameof(FunctionNodeImpl.Execute);
        _ = nameof(DynamicJson);
        _ = nameof(IRED.GlobalContext);

        var dict = new Dictionary<string, string>
        {
            ["f"] = "f-1"
        };

        var code = """
            var d = (Dictionary<string, string>)msg.Payload;
            d["v"] = "x";
            //msg["sub"] = 2;
            msg.sub = 2;
            return d;
            """;

        //Act
        var msg = await RunUsingTaskManager(NodesWorkflowBuilder.Create()
                                                .AddNext(new FunctionNode { Code = code })
                                            , new Core.NodeMsg { Payload = dict });

        //Assert
        msg.Payload.Should().BeOfType<Dictionary<string, string>>();
        var d = (Dictionary<string, string>)msg.Payload;
        d["f"].Should().Be("f-1");
        d["v"].Should().Be("x");
        msg.Context.Should().ContainKey("sub");
        msg.Context["sub"].Should().Be(2);
    }

    [Fact]
    public async Task Execute_LinqWork_ShouldWork()
    {
        //Arrange
        _ = nameof(FunctionNodeImpl.Execute);

        var input = new NodeMsg { Payload = "/x123/y432/required/" };

        var code = """
            return ((string)msg.Payload).Trim('/').Split('/').Last();
            """;

        //Act
        var msg = await ExecuteFunctionNode(code, input);

        //Assert
        msg.Payload.Should().Be("required");
    }

    [Fact]
    public async Task Execute_ArrayIndexWork_ShouldWork()
    {
        //Arrange
        _ = nameof(FunctionNodeImpl.Execute);

        var input = new NodeMsg { Payload = "/x123/y432/required/" };

        var code = """
            return ((string)msg.Payload).Trim('/').Split('/')[^1];
            """;

        //Act
        var msg = await ExecuteFunctionNode(code, input);

        //Assert
        msg.Payload.Should().Be("required");
    }
}
