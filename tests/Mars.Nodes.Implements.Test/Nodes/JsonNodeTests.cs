using FluentAssertions;
using Mars.Host.Shared.Templators;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Implements.Test.Services;

namespace Mars.Nodes.Implements.Test.Nodes;

public class JsonNodeTests : NodeServiceUnitTestBase
{
    readonly string json = """
                {
                    "name":"Dima",
                    "age":35
                }
                """;

    [Fact]
    public async Task Execute_AccessToObjectField_ShouldReturnField()
    {
        //Arrange
        _ = nameof(JsonNodeImpl.ParseString);
        var input = new NodeMsg() { Payload = json };

        var msg = await RunUsingTaskManager(NodesWorkflowBuilder.Create()
                                                .AddNext(new JsonNode())
                                            , input);
        msg.Payload.GetType().Should().Be<DynamicJson>();

        var redContext = RED.CreateContextForNode(RED.Nodes.Values.First(node => node.Node is JsonNode).Node, (FlowNodeImpl)RED.Nodes.Values.First(node => node is FlowNodeImpl));
        var setter = new VariableSetExpression { ValuePath = "msg.Payload", Expression = "msg.Payload.age", Operation = VariableSetOperation.Set };

        var ppt = VariableSetNodeImpl.CreateInterpreter(redContext, msg!);

        //Act
        var expressionResult = VariableSetNodeImpl.SetExpression(setter, ppt, redContext, msg!);

        //Assert
        expressionResult.Should().Be(35);
        expressionResult.GetType().Should().Be<int>();
    }

    [Fact]
    public async Task DirectCallSetExpression_AccessToObjectField_ShouldReturnField()
    {
        //Arrange
        _ = nameof(JsonNodeImpl.Execute);
        var input = new NodeMsg() { Payload = json };

        var msg = await RunUsingTaskManager(NodesWorkflowBuilder.Create()
                                                .AddNext(new JsonNode())
                                            , input);
        msg.Payload.GetType().Should().Be<DynamicJson>();

        //Act
        var ppt = new XInterpreter(pageContext: null, new() { { "payload", msg.Payload! } });
        var evalResult = ppt.Get.Eval("payload.age");

        //Assert
        evalResult.Should().Be(35);
        evalResult.GetType().Should().Be<int>();
    }

    [Fact]
    public async Task ChainCallWithVariableSetNode_AccessToObjectField_ShouldReturnField()
    {
        //Arrange
        _ = nameof(JsonNodeImpl.ParseString);
        var input = new NodeMsg() { Payload = json };

        //Act
        var msg = await RunUsingTaskManager(NodesWorkflowBuilder.Create()
                                                .AddNext(new JsonNode())
                                                .AddNext(new VariableSetNode()
                                                {
                                                    Setters = [new VariableSetExpression { ValuePath = "msg.Payload", Expression = "msg.Payload.age", Operation = VariableSetOperation.Set }]
                                                })
                                            , input);

        //Assert
        msg.Payload.GetType().Should().Be<int>();
        msg.Payload.Should().Be(35);

    }

}

public class JsonNodePartsTests
{
    [Fact]
    public void ToJsonString_ObjectToString_Success()
    {
        //Arrange
        _ = nameof(JsonNodeImpl.ToJsonString);
        var val = new
        {
            Name = "Dima"
        };

        string expect = @"{""name"":""Dima""}";

        //Act
        //Assert

        //Pure object
        string json = JsonNodeImpl.ToJsonString(val, false);

        Assert.Equal(expect, json);

        //Newtonsoft
        var newtonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(expect)!;

        string jsonFromNewtonObject = JsonNodeImpl.ToJsonString(newtonObject, false);

        Assert.Equal(expect, jsonFromNewtonObject);

        //System.Text
        var systemTextObject = System.Text.Json.JsonSerializer.Deserialize<object>(expect)!;

        string jsonFromSystemText = JsonNodeImpl.ToJsonString(systemTextObject, false);

        Assert.Equal(expect, jsonFromSystemText);
    }

    [Fact]
    public void ParseString_StringToObject_Success()
    {
        //Arrange
        _ = nameof(JsonNodeImpl.ParseString);
        var val = new
        {
            Name = "Dima"
        };

        string json = @"{""name"":""Dima""}";

        //Act
        var deserialized = JsonNodeImpl.ParseString(json)!;

        //Assert
        Assert.Equal(val.Name, deserialized["name"].ToString());

    }

}
