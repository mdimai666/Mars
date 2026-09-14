using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentAssertions;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements.Utils;
using Mars.Nodes.Tests.Services;
using NSubstitute;

namespace Mars.Nodes.Tests.Nodes;

public class InjectNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_PayloadField_GoesToMsgPayload()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "Payload", Value = "hello" }] };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Payload.Should().Be("hello");
    }

    [Fact]
    public async Task Execute_OtherFields_GoToMsgContext()
    {
        //Arrange
        var node = new InjectNode
        {
            Fields =
            [
                new() { Key = "Payload", Value = "hello" },
                new() { Key = "status", Value = "ok" },
                new() { Key = "count", VarType = "int", Value = "42" },
                new() { Key = "flag", VarType = "bool", Value = "true" },
                new() { Key = "grace", VarType = "double", Value = "1.5" },
            ]
        };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Payload.Should().Be("hello");
        msg.Get("status").Should().Be("ok");
        msg.Get("count").Should().Be(42);
        msg.Get("flag").Should().Be(true);
        msg.Get("grace").Should().Be(1.5);
    }

    [Fact]
    public async Task Execute_ArrayField_GoesToMsgContext()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "items", VarType = "int[]", Value = "[1,2,3]" }] };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Get("items").Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task Execute_DefaultNode_ReturnsTimestamp()
    {
        //Arrange
        var node = new InjectNode();

        //Act
        var before = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var msg = await ExecuteNode(node);
        var after = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        //Assert
        var payload = msg.Payload.Should().BeOfType<long>().Subject;
        payload.Should().BeInRange(before, after);
    }

    [Fact]
    public async Task Execute_TimestampFieldWithoutValue_ReturnsNow()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "Payload", VarType = VarNode.TimestampTypeName }] };

        //Act
        var before = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var msg = await ExecuteNode(node);
        var after = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        //Assert
        var payload = msg.Payload.Should().BeOfType<long>().Subject;
        payload.Should().BeInRange(before, after);
    }

    [Fact]
    public async Task Execute_TimestampFieldWithValue_ReturnsThatValue()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "Payload", VarType = VarNode.TimestampTypeName, Value = "1700000000000" }] };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Payload.Should().Be(1700000000000L);
    }

    [Fact]
    public async Task Execute_EmptyStringValue_ReturnsEmptyString()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "Payload", VarType = "string", Value = "" }] };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Payload.Should().Be("");
    }

    [Fact]
    public async Task Execute_NoPayloadField_KeepsPayloadUntouched()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "status", Value = "ok" }] };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Payload.Should().BeNull();
        msg.Get("status").Should().Be("ok");
    }

    [Fact]
    public async Task Execute_EmptyFields_LeavesMsgUntouched()
    {
        //Arrange
        var node = new InjectNode { Fields = [] };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Payload.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithoutFields_UsesDefaultTimestampField()
    {
        //Arrange
        var json = """{"TypeId":"core.InjectNode","RunAtStartup":true}""";

        //Act
        var node = JsonSerializer.Deserialize<Node>(json, _jsonSerializerOptions) as InjectNode;

        //Assert
        node.Should().NotBeNull();
        node!.RunAtStartup.Should().BeTrue();
        node.Fields.Should().HaveCount(1);
        node.Fields[0].Key.Should().Be(InjectNode.PayloadKey);
        node.Fields[0].VarType.Should().Be(VarNode.TimestampTypeName);
    }

    [Fact]
    public void Serialize_Fields_RoundTrips()
    {
        //Arrange
        var node = new InjectNode
        {
            Fields =
            [
                new() { Key = "Payload", Value = "hello" },
                new() { Key = "count", VarType = "int", Value = "5" },
            ]
        };

        //Act
        var json = JsonSerializer.Serialize<Node>(node, _jsonSerializerOptions);
        var restored = JsonSerializer.Deserialize<Node>(json, _jsonSerializerOptions) as InjectNode;

        //Assert
        restored!.Fields.Should().HaveCount(2);
        restored.Fields[0].Key.Should().Be("Payload");
        restored.Fields[0].Value.Should().Be("hello");
        restored.Fields[1].VarType.Should().Be("int");
    }

    [Fact]
    public void Validate_DuplicateKeys_ReportsError()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "status" }, new() { Key = "Status" }] };

        //Act
        var results = node.Validate(new ValidationContext(node)).ToList();

        //Assert
        results.Should().Contain(r => r.ErrorMessage!.Contains("duplicated"));
    }

    [Fact]
    public void Validate_BadKeyAndType_ReportsErrors()
    {
        //Arrange
        var node = new InjectNode
        {
            Fields =
            [
                new() { Key = "bad key" },
                new() { Key = "value", VarType = "somethingElse" },
            ]
        };

        //Act
        var results = node.Validate(new ValidationContext(node)).ToList();

        //Assert
        results.Should().Contain(r => r.ErrorMessage!.Contains("identifier"));
        results.Should().Contain(r => r.ErrorMessage!.Contains("not supported"));
    }

    [Fact]
    public void Validate_DefaultNode_HasNoErrors()
    {
        //Arrange
        var node = new InjectNode();

        //Act
        var results = node.Validate(new ValidationContext(node)).ToList();

        //Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void BindRootPaths_ReplacesResolvableMsgPath()
    {
        //Arrange
        var msg = new NodeMsg { Payload = "abc" };
        var scope = new ExpressionScope(Substitute.For<IRuntimeNodeScope>(), msg);
        var interpreter = InputValueResolver.CreateInterpreter(scope.Rns, msg);

        //Act
        var bound = InputValueResolver.BindRootPaths("msg.Payload.Count() + 1", interpreter, scope);

        //Assert
        bound.Should().Be("msg_Payload.Count() + 1");
    }

    [Fact]
    public async Task Execute_ExpressionField_EvaluatesArithmetic()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "count", VarType = "int", ValueKind = InputValueKind.Expression, Value = "2 + 3" }] };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Get("count").Should().Be(5);
    }

    [Fact]
    public async Task Execute_ExpressionField_ReadsMsgPayload()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "copy", ValueKind = InputValueKind.Expression, Value = "msg.Payload" }] };

        //Act
        var msg = await ExecuteNode(node, new NodeMsg { Payload = "hello" });

        //Assert
        msg.Get("copy").Should().Be("hello");
    }

    [Fact]
    public async Task Execute_ExpressionField_ReadsMsgContext()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "copy", ValueKind = InputValueKind.Expression, Value = "msg.status" }] };
        var input = new NodeMsg();
        input.Set("status", "ok");

        //Act
        var msg = await ExecuteNode(node, input);

        //Assert
        msg.Get("copy").Should().Be("ok");
    }

    [Fact]
    public async Task Execute_ExpressionField_LinqOnPayloadString()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "len", VarType = "int", ValueKind = InputValueKind.Expression, Value = "msg.Payload.Count() + 1" }] };

        //Act
        var msg = await ExecuteNode(node, new NodeMsg { Payload = "abc" });

        //Assert
        msg.Get("len").Should().Be(4);
    }

    [Fact]
    public async Task Execute_ExpressionField_ConvertsResultToVarType()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "asText", VarType = "string", ValueKind = InputValueKind.Expression, Value = "2 + 3" }] };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        msg.Get("asText").Should().Be("5");
    }

    [Fact]
    public async Task Execute_ExpressionField_StringLiteralNotRewritten()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "x", ValueKind = InputValueKind.Expression, Value = "\"msg.Payload\"" }] };

        //Act
        var msg = await ExecuteNode(node, new NodeMsg { Payload = "hello" });

        //Assert
        msg.Get("x").Should().Be("msg.Payload");
    }

    [Fact]
    public async Task Execute_ExpressionField_BadExpression_Throws()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "x", ValueKind = InputValueKind.Expression, Value = "1 +" }] };

        //Act
        var act = () => ExecuteNode(node);

        //Assert
        await act.Should().ThrowAsync<NodeExecuteException>();
    }

    [Fact]
    public async Task Execute_ExpressionField_NullResultForValueType_Throws()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "x", VarType = "int", ValueKind = InputValueKind.Expression, Value = "msg.Missing" }] };

        //Act
        var act = () => ExecuteNode(node);

        //Assert
        await act.Should().ThrowAsync<NodeExecuteException>();
    }

    [Fact]
    public async Task Execute_UnknownValueKind_Throws()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "x", ValueKind = "somethingElse", Value = "1" }] };

        //Act
        var act = () => ExecuteNode(node);

        //Assert
        await act.Should().ThrowAsync<NodeExecuteException>();
    }

    [Fact]
    public void Validate_UnknownValueKind_ReportsError()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "x", ValueKind = "somethingElse", Value = "1" }] };

        //Act
        var results = node.Validate(new ValidationContext(node)).ToList();

        //Assert
        results.Should().Contain(r => r.ErrorMessage!.Contains("Value kind"));
    }

    [Fact]
    public void Validate_EmptyExpression_ReportsError()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "x", ValueKind = InputValueKind.Expression, Value = "  " }] };

        //Act
        var results = node.Validate(new ValidationContext(node)).ToList();

        //Assert
        results.Should().Contain(r => r.ErrorMessage!.Contains("expression must not be empty"));
    }

    [Fact]
    public void Serialize_ValueKind_RoundTrips()
    {
        //Arrange
        var node = new InjectNode { Fields = [new() { Key = "x", VarType = "int", ValueKind = InputValueKind.Expression, Value = "1 + 1" }] };

        //Act
        var json = JsonSerializer.Serialize<Node>(node, _jsonSerializerOptions);
        var restored = JsonSerializer.Deserialize<Node>(json, _jsonSerializerOptions) as InjectNode;

        //Assert
        json.Should().Contain("ValueKind");
        restored!.Fields[0].ValueKind.Should().Be(InputValueKind.Expression);
    }
}
