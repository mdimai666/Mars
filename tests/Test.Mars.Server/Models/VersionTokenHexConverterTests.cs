using System.Text.Json;
using Mars.Host.Shared.JsonConverters;
using Mars.Host.Shared.Models;
using FluentAssertions;

namespace Test.Mars.Host.Models;

public class VersionTokenHexConverterTests
{
    class JsonTestTokenVersionRequestClass
    {
        public required string Name { get; set; }

        public VersionTokenHex Version { get; set; } = default!;
    }

    JsonSerializerOptions opt = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new VersionTokenHexJsonConverter(),
        },
    };

    [Fact]
    public void FromJson()
    {
        // Arrange
        var inputWithToken = @"{""name"":""username"",""version"":""FF""}";
        var inputWitoutToken = @"{""name"":""username""}";

        var itemWithToken = new JsonTestTokenVersionRequestClass
        {
            Name = "username",
            Version = new VersionTokenHex(255)
        };
        var itemWitoutToken = new JsonTestTokenVersionRequestClass
        {
            Name = "username",
        };

        // Act
        var itemWithTokenObject = JsonSerializer.Deserialize<JsonTestTokenVersionRequestClass>(inputWithToken, opt);
        var itemWitoutTokenObject = JsonSerializer.Deserialize<JsonTestTokenVersionRequestClass>(inputWitoutToken, opt);

        // Assert
        itemWithTokenObject.Should().BeEquivalentTo(itemWithToken);
        itemWitoutTokenObject.Should().BeEquivalentTo(itemWitoutToken);
    }

    [Fact]
    public void ToJson()
    {
        // Arrange
        var expectedWithToken = @"{""name"":""username"",""version"":""FF""}";
        var expectedWitoutToken = @"{""name"":""username"",""version"":null}";

        var itemWithToken = new JsonTestTokenVersionRequestClass
        {
            Name = "username",
            Version = new VersionTokenHex(255)
        };
        var itemWitoutToken = new JsonTestTokenVersionRequestClass
        {
            Name = "username",
        };

        // Act
        var itemWithTokenJson = JsonSerializer.Serialize(itemWithToken, opt);
        var itemWitoutTokenJson = JsonSerializer.Serialize(itemWitoutToken, opt);

        // Assert
        itemWithTokenJson.Should().BeEquivalentTo(expectedWithToken);
        itemWitoutTokenJson.Should().BeEquivalentTo(expectedWitoutToken);
        itemWitoutToken.Version.Should().BeNull();
    }
}
