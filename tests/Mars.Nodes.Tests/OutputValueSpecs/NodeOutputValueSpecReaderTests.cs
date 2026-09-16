using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Network;

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
            new OutputValueSpec("MqttNodeMessagePaylad", "object"),
            new OutputValueSpec("MqttNodeMessagePaylad.ContentType", "string"),
            new OutputValueSpec("MqttNodeMessagePaylad.Dup", "bool"),
            new OutputValueSpec("MqttNodeMessagePaylad.MessageExpiryInterval", "object"),
            new OutputValueSpec("MqttNodeMessagePaylad.QoS", "object"),
            new OutputValueSpec("MqttNodeMessagePaylad.ResponseTopic", "string"),
            new OutputValueSpec("MqttNodeMessagePaylad.Retain", "bool"),
            new OutputValueSpec("MqttNodeMessagePaylad.Topic", "string"),
            new OutputValueSpec("MqttNodeMessagePaylad.Payload", "string"));
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
