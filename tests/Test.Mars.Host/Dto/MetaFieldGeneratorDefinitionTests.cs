using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;

namespace Test.Mars.Host.Dto;

public class MetaFieldGeneratorDefinitionTests
{
    [Fact]
    public void FromOptions_ReadsTypeAndParams()
    {
        var options = new JsonObject
        {
            ["generator"] = new JsonObject
            {
                ["type"] = MetaFieldGeneratorCatalog.Sequence,
                ["params"] = new JsonObject { ["prefix"] = "ВУ" },
            },
        };

        var definition = MetaFieldGeneratorDefinition.FromOptions(options);

        definition.Should().NotBeNull();
        definition!.Type.Should().Be(MetaFieldGeneratorCatalog.Sequence);
        definition.Params.Should().NotBeNull();
        definition.Params!["prefix"]!.GetValue<string>().Should().Be("ВУ");
    }

    [Fact]
    public void FromOptions_NoGenerator_ReturnsNull()
    {
        MetaFieldGeneratorDefinition.FromOptions(null).Should().BeNull();
        MetaFieldGeneratorDefinition.FromOptions(new JsonObject()).Should().BeNull();
    }

    [Fact]
    public void FromOptions_EmptyType_ReturnsNull()
    {
        var options = new JsonObject
        {
            ["generator"] = new JsonObject { ["type"] = "" },
        };

        MetaFieldGeneratorDefinition.FromOptions(options).Should().BeNull();
    }
}
