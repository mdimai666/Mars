using System.Text.Json;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.WebApp;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.JsonConverters;

public class NodeJsonConverterTests
{
    public NodeJsonConverterTests()
    {
        NodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);
        NodesLocator.RegisterAssembly(typeof(CssCompilerNode).Assembly);
        NodesLocator.RefreshDict();

        //NodeFormsLocator.RegisterAssembly(typeof(InjectNodeForm).Assembly);
        //NodeFormsLocator.RefreshDict();
    }

    [Fact]
    public void JsonConverter_SmallNodes_Success()
    {
        //Arrange
        _ = nameof(NodeJsonConverter);
        Node[] nodes = [
            new InjectNode() {
                Name = "node5f0fa72f-4149-4de3-9d3a-1a53ec989a94",
                StartupDelayMillis = 666,
            },
            new TemplateNode(),
        ];
        var nodesJson = JsonSerializer.Serialize(nodes);

        //Act
        var deserialized = JsonSerializer.Deserialize<Node[]>(nodesJson);

        //Assert
        var _injectNode = deserialized[0];
        _injectNode.Should().NotBeNull();
        _injectNode.Should().BeOfType<InjectNode>();
        var injectNode = (InjectNode)_injectNode;
        injectNode.Should().BeEquivalentTo((InjectNode)nodes[0], options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<InjectNode>()
            .ExcludingMissingMembers());
        injectNode.StartupDelayMillis.Should().Be(((InjectNode)nodes[0]).StartupDelayMillis); // test of test
    }
}
