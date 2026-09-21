using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Common;
using Mars.Nodes.Core.Implements.Nodes.Functions;
using Mars.Nodes.Core.Implements.Nodes.Network;
using Mars.Nodes.Core.StringFunctions;

namespace Mars.Nodes.Tests.OutputValueSpecs;

public class NodeOutputValueSpecReaderTests
{
    [Fact]
    public void Read_InjectNode_ReturnsFieldSpecs()
    {
        var node = new InjectNode
        {
            Fields =
            [
                new() { Key = "Payload", Value = "hello" },
                new() { Key = "count", VarType = "int", Value = "42" },
                new() { Key = "tags", VarType = "string[]", Value = "[]" },
            ]
        };

        var specs = NodeOutputValueSpecReader.Read(node);

        specs.Should().Equal(
            new OutputValueSpec("Payload", "string"),
            new OutputValueSpec("count", "int"),
            new OutputValueSpec("tags", "string[]"));
    }

    [Fact]
    public void Read_InjectNode_SkipsFieldsWithoutKey()
    {
        var node = new InjectNode { Fields = [new() { Key = " " }, new() { Key = "ok" }] };

        NodeOutputValueSpecReader.Read(node).Should().Equal(new OutputValueSpec("ok", "string"));
    }

    [Fact]
    public void Read_SwitchNode_DeclaresNothing()
    {
        NodeOutputValueSpecReader.Read(new SwitchNode()).Should().BeEmpty();
    }

    [Fact]
    public void ReadStatics_MqttInNodeImplement_DeclaresPayloadAndReceivedMessage()
    {
        NodeOutputValueSpecReader.ReadStatics(typeof(MqttInNodeImpl)).Should().Equal(
            new OutputValueSpec("Payload", "string"),
            new OutputValueSpec("MqttNodeMessagePayload", "object"),
            new OutputValueSpec("MqttNodeMessagePayload.ContentType", "string"),
            new OutputValueSpec("MqttNodeMessagePayload.Dup", "bool"),
            new OutputValueSpec("MqttNodeMessagePayload.MessageExpiryInterval", "object"),
            new OutputValueSpec("MqttNodeMessagePayload.QoS", "object"),
            new OutputValueSpec("MqttNodeMessagePayload.ResponseTopic", "string"),
            new OutputValueSpec("MqttNodeMessagePayload.Retain", "bool"),
            new OutputValueSpec("MqttNodeMessagePayload.Topic", "string"),
            new OutputValueSpec("MqttNodeMessagePayload.Payload", "string"));
    }

    [Fact]
    public void ReadStatics_AttributeWithOutputPort_SetsPortOnEverySpec()
    {
        NodeOutputValueSpecReader.ReadStatics(typeof(SecondOutputNode)).Should().Equal(
            new OutputValueSpec("Payload", "object", 1),
            new OutputValueSpec("Payload.Status", "string", 1),
            new OutputValueSpec("Payload.Code", "int", 1));
    }

    [Fact]
    public void ReadStatics_AllOutputPortsAttribute_UsesMinusOne()
    {
        NodeOutputValueSpecReader.ReadStatics(typeof(AllOutputsNode)).Should().Equal(
            new OutputValueSpec("Payload", "object", OutputValueSpec.AllOutputPorts),
            new OutputValueSpec("Payload.Status", "string", OutputValueSpec.AllOutputPorts),
            new OutputValueSpec("Payload.Code", "int", OutputValueSpec.AllOutputPorts));
    }

    [Fact]
    public void ReadStatics_AttributeWithName_UsesSlotName()
    {
        NodeOutputValueSpecReader.ReadStatics(typeof(NamedSlotNode)).Should().Equal(
            new OutputValueSpec("RequestInfo", "object"),
            new OutputValueSpec("RequestInfo.Status", "string"),
            new OutputValueSpec("RequestInfo.Code", "int"));
    }

    [Fact]
    public void ReadStatics_SeveralAttributes_ReturnsAllSlots()
    {
        NodeOutputValueSpecReader.ReadStatics(typeof(MultiSlotNode)).Should().Equal(
            new OutputValueSpec("Payload", "object"),
            new OutputValueSpec("Payload.Status", "string"),
            new OutputValueSpec("Payload.Code", "int"),
            new OutputValueSpec("Text", "string"));
    }

    [Fact]
    public void ReadStatics_TypeWithoutAttributes_ReturnsEmpty()
    {
        NodeOutputValueSpecReader.ReadStatics(typeof(SwitchNode)).Should().BeEmpty();
    }

    [Fact]
    public void Fallback_IsObjectPayload()
    {
        NodeOutputValueSpecReader.Fallback.Should().Equal(new OutputValueSpec("Payload", "object"));
    }

    [Fact]
    public void Read_HttpRequestNode_PayloadTypeFollowsReturnResponse()
    {
        NodeOutputValueSpecReader.Read(new HttpRequestNode { ReturnResponse = HttpRequestNode.ReturnResponseType.String })
            .Should().Equal(new OutputValueSpec("Payload", "string"));

        NodeOutputValueSpecReader.Read(new HttpRequestNode { ReturnResponse = HttpRequestNode.ReturnResponseType.Auto })
            .Should().Equal(new OutputValueSpec("Payload", "object", Description: "by Content-Type: JSON, string or bytes"));

        NodeOutputValueSpecReader.Read(new HttpRequestNode { ReturnResponse = HttpRequestNode.ReturnResponseType.Object })
            .Should().Equal(new OutputValueSpec("Payload", "object", Description: "parsed JSON"));
    }

    [Fact]
    public void ReadStatics_HttpRequestNodeImpl_DeclaresRequestInfoSlot()
    {
        var specs = NodeOutputValueSpecReader.ReadStatics(typeof(HttpRequestNodeImpl));

        specs.Should().Contain(new OutputValueSpec("HttpRequestInfo", "object"));
        specs.Should().Contain(new OutputValueSpec("HttpRequestInfo.StatusCode", "int"));
        specs.Should().Contain(new OutputValueSpec("HttpRequestInfo.Response.StatusCode", "int"));
        specs.Should().Contain(new OutputValueSpec("HttpRequestInfo.Response.Content", "string"));
    }

    [Fact]
    public void Read_EndpointNode_PayloadTypeFollowsInputModel()
    {
        NodeOutputValueSpecReader.Read(new EndpointNode { EndpointInputModel = EndpointInputModelType.String })
            .Should().Equal(new OutputValueSpec("Payload", "string"));

        NodeOutputValueSpecReader.Read(new EndpointNode { EndpointInputModel = EndpointInputModelType.JsonSchema })
            .Should().Equal(new OutputValueSpec("Payload", "object", Description: "JSON validated by schema"));
    }

    [Fact]
    public void Read_HttpInNode_DeclaresObjectPayloadWithDescription()
    {
        NodeOutputValueSpecReader.Read(new HttpInNode())
            .Should().Equal(new OutputValueSpec("Payload", "object",
                Description: "request body: string, JSON (JsonNode) or form-data — by request Content-Type"));
    }

    [Fact]
    public void Read_HttpInFormSaveFilesNode_BranchesBySaveInMediaFiles()
    {
        NodeOutputValueSpecReader.Read(new HttpInFormSaveFilesNode())
            .Should().Equal(
                new OutputValueSpec("Payload", "string[]", Description: "saved file paths"),
                new OutputValueSpec("Payload[]", "string"));

        var media = NodeOutputValueSpecReader.Read(new HttpInFormSaveFilesNode { SaveInMediaFiles = true });

        media.Should().Contain(new OutputValueSpec("Payload", "object", Description: "saved media files (FileListItem[])"));
        media.Should().Contain(new OutputValueSpec("Payload[].Name", "string"));
        media.Should().Contain(new OutputValueSpec("Payload[].FileVirtualPath", "string"));
    }

    [Fact]
    public void ReadStatics_CatchErrorNodeImpl_DeclaresExceptionPayloadWithoutRecursion()
    {
        var specs = NodeOutputValueSpecReader.ReadStatics(typeof(CatchErrorNodeImpl));

        specs.Should().Contain(new OutputValueSpec("Payload", "object"));
        specs.Should().Contain(new OutputValueSpec("Payload.Message", "string"));
        specs.Should().Contain(new OutputValueSpec("Payload.StackTrace", "string"));
        specs.Should().Contain(new OutputValueSpec("Payload.InnerException", "object"));
        specs.Should().NotContain(s => s.Path.StartsWith("Payload.InnerException.", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadStatics_ExecNodeImpl_DeclaresStringPayload()
    {
        NodeOutputValueSpecReader.ReadStatics(typeof(ExecNodeImpl))
            .Should().Equal(new OutputValueSpec("Payload", "string"));
    }

    [Fact]
    public void Read_StringNode_PayloadTypeFollowsLastOperation()
    {
        NodeOutputValueSpecReader.Read(new StringNode())
            .Should().Equal(new OutputValueSpec("Payload", "string"));

        NodeOutputValueSpecReader.Read(new StringNode
            {
                Operations = [new() { Method = nameof(StringNodeOperationUtils.Split) }]
            })
            .Should().Equal(new OutputValueSpec("Payload", "string[]"));

        NodeOutputValueSpecReader.Read(new StringNode
            {
                Operations =
                [
                    new() { Method = nameof(StringNodeOperationUtils.Split) },
                    new() { Method = nameof(StringNodeOperationUtils.Join) },
                ]
            })
            .Should().Equal(new OutputValueSpec("Payload", "string"));
    }

    [Fact]
    public void Read_StringNode_WithoutOperations_DeclaresNothing()
    {
        NodeOutputValueSpecReader.Read(new StringNode { Operations = [] }).Should().BeEmpty();
    }

    [NodeOutputValueSpec(typeof(NamedSlotDto), Name = "RequestInfo")]
    private sealed class NamedSlotNode;

    private sealed class NamedSlotDto
    {
        public string Status { get; set; } = "";
        public int Code { get; set; }
    }

    [NodeOutputValueSpec(typeof(NamedSlotDto))]
    [NodeOutputValueSpec(typeof(string), Name = "Text")]
    private sealed class MultiSlotNode;

    [NodeOutputValueSpec(typeof(NamedSlotDto), OutputPort = 1)]
    private sealed class SecondOutputNode;

    [NodeOutputValueSpec(typeof(NamedSlotDto), OutputPort = OutputValueSpec.AllOutputPorts)]
    private sealed class AllOutputsNode;
}
