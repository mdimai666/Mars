using Mars.Host.Shared.Templators;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Implements.Test.Services;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Nodes;

public class VariableSetNodeTests : NodeServiceUnitTestBase
{

    [Theory]
    [InlineData("msg.Payload", "25", 25)]
    [InlineData("msg.Payload", "25*25", 25 * 25)]
    [InlineData("msg.Payload", "\"1\"+\"2\"", "12")]
    public async Task Execute_MsgPayloadSet_Success(string valuePath, string expression, object expectValue)
    {
        //Arrange
        _ = nameof(VariableSetNodeImpl.Execute);
        var node = SetupNode(valuePath, expression);

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Payload.Should().Be(expectValue);
    }

    [Theory]
    [InlineData("GlobalContext.q", "25", 25)]
    [InlineData("GlobalContext.q", "true", true)]
    [InlineData("GlobalContext.q", "\"text\"", "text")]
    public async Task Execute_GlobalContextSet_Success(string valuePath, string expression, object expectValue)
    {
        //Arrange
        _ = nameof(VariableSetNodeImpl.Execute);
        var node = SetupNode(valuePath, expression);

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        RED.GlobalContext.GetValue(valuePath.Split('.', 2)[1]).Should().Be(expectValue);
    }

    [Fact]
    public async Task Execute_GlobalContextSetSubPropertyByPath_Success()
    {
        //Arrange
        _ = nameof(VariableSetNodeImpl.Execute);
        var node = SetupNode("GlobalContext.q.Payload", "25");
        RED.GlobalContext.SetValue("q", new NodeMsg { Payload = 1 });

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        RED.GlobalContext.GetValue<NodeMsg>("q").Payload.Should().Be(25);
    }

    [Theory]
    [InlineData("FlowContext.q", "25", 25)]
    [InlineData("FlowContext.q", "true", true)]
    [InlineData("FlowContext.q", "\"text\"", "text")]
    public async Task Execute_FlowContextSet_Success(string valuePath, string expression, object expectValue)
    {
        //Arrange
        _ = nameof(VariableSetNodeImpl.Execute);
        var node = SetupNode(valuePath, expression);
        var flowNode = new FlowNode();
        RED.FlowContexts.Add(flowNode.Id, new());
        var flowContext = RED.FlowContexts[flowNode.Id];

        //Act
        var msg = await ExecuteNodeEx(node, flowNode: flowNode);

        //Assert
        flowContext.GetValue(valuePath.Split('.', 2)[1]).Should().Be(expectValue);
    }

    [Fact]
    public async Task Execute_FlowContextSetSubPropertyByPath_Success()
    {
        //Arrange
        _ = nameof(VariableSetNodeImpl.Execute);
        var node = SetupNode("FlowContext.q.Payload", "25");
        var flowNode = new FlowNode();
        RED.FlowContexts.Add(flowNode.Id, new());
        var flowContext = RED.FlowContexts[flowNode.Id];
        flowContext.SetValue("q", new NodeMsg { Payload = 1 });

        //Act
        var msg = await ExecuteNodeEx(node, flowNode: flowNode);

        //Assert
        flowContext.GetValue<NodeMsg>("q").Payload.Should().Be(25);
    }

    public static IEnumerable<object[]> SetVarData() => [
        ["VarNode.q", "int", "25", 25],
        ["VarNode.q", "int[]", "[1,2,3]", (int[])[1, 2, 3]],
        ["VarNode.q", "bool", "true", true],
        ["VarNode.q", "string", "\"text\"", "text"],
        ["VarNode.q", "string[]", "[\"text\", \"s1\"]", (string[])["text", "s1"]],
        ["VarNode.q", "Guid", "\"0ac104a5-d439-4f1c-bfaf-f85268e8968c\"", new Guid("0ac104a5-d439-4f1c-bfaf-f85268e8968c")],
        //["VarNode.q", "Guid[]", "[\"5473a8fb-01b9-40e2-8eba-d92134d8fdc1\", \"daaef7ca-d92b-4059-9c04-59166c5c2ef2\"]",
        //    (Guid[])[new Guid("5473a8fb-01b9-40e2-8eba-d92134d8fdc1"),new Guid("daaef7ca-d92b-4059-9c04-59166c5c2ef2")]],
    ];

    [Theory]
    [MemberData(nameof(SetVarData))]
    public async Task Execute_VarNodeSet_Success(string valuePath, string varType, string valueString, object expectValue)
    {
        //var g = Guid.NewGuid();
        //var j = JsonSerializer.Serialize(g);

        //Arrange
        _ = nameof(VariableSetNodeImpl.Execute);
        var node = SetupNode(valuePath, valueString);
        var varNode = new VarNode() { VarType = varType, Name = "q", Value = VarNode.ResolveDefault(varType) };
        varNode.SetByJsonString(valueString);

        //Act
        var msg = await ExecuteNodeEx(node, varNode: varNode);

        //Assert
        varNode.Value.Should().BeEquivalentTo(expectValue);
    }

    [Theory]
    //[InlineData("int[]", "new int[]{ 1,2,3 }", (int[])[1, 2, 3])]
    [InlineData("int[]", "[ 1,2,3 ]", (int[])[1, 2, 3])]
    [InlineData("int[]", "[ 1+1,2+2,3+3 ]", (int[])[1 + 1, 2 + 2, 3 + 3])]
    [InlineData("long[]", "[ 1L,2L,3L ]", (long[])[1L, 2L, 3L])]
    [InlineData("float[]", "[ 0.1f,2f,3f ]", (float[])[0.1f, 2f, 3f])]
    [InlineData("double[]", "[ 1d,2d,3d ]", (double[])[1d, 2d, 3d])]
    //[InlineData("decimal[]", "[ 1M,2M,3M ]", (decimal[])[ 1M, 2M, 3M ])] not work on C#
    [InlineData("bool[]", "[ false, true, true ]", (bool[])[false, true, true])]
    [InlineData("string[]", "[ \"x1\", \"x2\" ]", (string[])["x1", "x2"])]
    public void InitArray_Ctor_Success(string varType, string expression, object expect)
    {
        //Arrange
        var ppt = new XInterpreter();

        //Act
        var replaced = VariableSetNodeImpl.SmartReplaceArrayInitializer(expect.GetType(), expression);
        var result = ppt.Get.Eval(replaced);

        //Assert
        result.Should().BeEquivalentTo(expect);
        VarNode.GetPureArrayInitializerPrefix(result.GetType()).Should().Be(varType);
    }

    [Fact]
    public async Task Execute_CompleteExpressionThatAccessesEverything_SuccessForGlobalAndFlowAndVarNodeContexts()
    {
        //Arrange
        _ = nameof(VariableSetNodeImpl.Execute);
        _ = nameof(VariableSetNodeImpl.CreateInterpreter);

        var flowNode = new FlowNode();
        RED.FlowContexts.Add(flowNode.Id, new());
        var flowContext = RED.FlowContexts[flowNode.Id];
        flowContext.SetValue("f1", "hi, ");

        var varNode = new VarNode() { VarType = "string", Name = "var1", Value = ", _end!" };

        RED.GlobalContext.SetValue("v1", 1);
        //var node = SetupNode("msg.Payload", "GlobalContext.v1 + 2");
        var node = new VariableSetNode
        {
            Setters = [
                new () { ValuePath = "GlobalContext.v1", Expression = "2+2" },
                new () { ValuePath = "msg.Payload", Expression = "GlobalContext.v1 + 3" },
                new () { ValuePath = "FlowContext.f1", Expression = "FlowContext.f1 + \"xx\" + VarNode.var1" },
            ]
        };

        //Act
        var result = await ExecuteNodeEx(node, flowNode: flowNode, varNode: varNode);

        //Assert
        RED.GlobalContext.GetValue<int>("v1").Should().Be(4);
        result.Msg.Payload.Should().Be(7);
        RED.FlowContexts[flowNode.Id].GetValue<string>("f1").Should().BeEquivalentTo("hi, xx, _end!");
    }

    VariableSetNode SetupNode(string path, string expression)
    {
        var input = new NodeMsg() { Payload = 1 };

        var node = new VariableSetNode
        {
            Setters = [
                new () { ValuePath = path, Expression = expression },
            ]
        };

        return node;
    }

    #region UTILS
    [Fact]
    public void SetProperty_Set_Success()
    {
        var x = new NodeMsg { Payload = 0 };

        VariableSetNodeImpl.SetProperty(x, "Payload", 1);

        x.Payload.Should().Be(1);
    }

    public class SubFieldedItem
    {
        public int Property1 { get; set; }
        public int Field1;
    }

    [Fact]
    public void SetProperty_SetSubProperty_Success()
    {
        var x = new NodeMsg { Payload = new SubFieldedItem { Property1 = 0 } };

        VariableSetNodeImpl.SetProperty(x, "Payload.Property1", 1);

        ((SubFieldedItem)x.Payload).Property1.Should().Be(1);
    }

    [Fact]
    public void SetProperty_SetSubField_Success()
    {
        var x = new NodeMsg { Payload = new SubFieldedItem { Field1 = 0 } };

        VariableSetNodeImpl.SetProperty(x, "Payload.Field1", 1);

        ((SubFieldedItem)x.Payload).Field1.Should().Be(1);
    }
    #endregion
}
