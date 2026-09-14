using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Tests.Services;

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
}
