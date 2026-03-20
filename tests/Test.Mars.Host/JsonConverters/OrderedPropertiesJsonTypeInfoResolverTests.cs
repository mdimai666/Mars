using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Host.Shared.JsonConverters;

namespace Test.Mars.Host.JsonConverters;

public class OrderedPropertiesJsonTypeInfoResolverTests
{
    [Fact]
    public void Serialize_WithInheritance_ShouldPlaceBasePropertiesFirst()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new OrderedPropertiesJsonTypeInfoResolver(),
            WriteIndented = false
        };

        var model = new DerivedModel
        {
            BaseProp = "base",
            DerivedProp1 = "d1",
            DerivedProp2 = "d2"
        };

        // Act
        var json = JsonSerializer.Serialize(model, options);

        // Assert
        // сначала base, потом derived
        Assert.Equal(
            "{\"base_prop\":\"base\",\"derived_prop1\":\"d1\",\"derived_prop2\":\"d2\"}",
            json);
    }

    private class BaseModel
    {
        [JsonPropertyName("base_prop")]
        public string BaseProp { get; set; } = default!;
    }

    private class DerivedModel : BaseModel
    {
        [JsonPropertyName("derived_prop1")]
        public string DerivedProp1 { get; set; } = default!;

        [JsonPropertyName("derived_prop2")]
        public string DerivedProp2 { get; set; } = default!;
    }
}
