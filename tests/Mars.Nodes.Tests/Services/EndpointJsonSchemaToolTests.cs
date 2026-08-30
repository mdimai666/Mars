using FluentAssertions;
using Mars.Nodes.Core.Implements.Utils;

namespace Mars.Nodes.Implements.Test.Services;

public class EndpointJsonSchemaToolTests
{
    public const string _exampleUserObject = """
                    {
                      "email": "test@test.com",
                      "age": 30,
                      "admin": true
                    }
                    """;

    [Fact]
    public void ValidateAndFilter_FilterField_ShouldWork()
    {
        //Arrange
        var schema = new EndpointJsonSchemaTool.SimpleJsonSchema
        {
            Type = "object",
            Required = ["email"],
            Properties = new()
            {
                ["email"] = new() { Type = "string" },
                ["age"] = new() { Type = "integer" },
            }
        };

        var input = _exampleUserObject;

        //Act
        var result = EndpointJsonSchemaTool.ValidateAndFilter(schema, input);

        //Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.ValidatedJson.AsObject().ContainsKey("admin").Should().BeFalse();
    }

    [Fact]
    public void ValidateAndFilter_MissingRequiredField_ShouldFail()
    {
        //Arrange
        var schema = new EndpointJsonSchemaTool.SimpleJsonSchema
        {
            Type = "object",
            Required = ["email", "name"],
            Properties = new()
            {
                ["email"] = new() { Type = "string" },
                ["age"] = new() { Type = "integer" },
                ["name"] = new() { Type = "string" },
            }
        };

        var input = _exampleUserObject;

        //Act
        var result = EndpointJsonSchemaTool.ValidateAndFilter(schema, input);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Should().Match("*required");
    }

    [Fact]
    public void ValidateAndFilter_UsingJsonSchemaFromString_ShouldOk()
    {
        //Arrange
        var schemaJson = """
                        {
                          "$id": "https://example.com/address.schema.json",
                          "$schema": "https://json-schema.org/draft/2020-12/schema",
                          "description": "An address similar to http://microformats.org/wiki/h-card",
                          "type": "object",
                          "properties": {
                            "postOfficeBox": {
                              "type": "string"
                            },
                            "extendedAddress": {
                              "type": "string"
                            },
                            "streetAddress": {
                              "type": "string"
                            },
                            "locality": {
                              "type": "string"
                            },
                            "region": {
                              "type": "string"
                            },
                            "postalCode": {
                              "type": "string"
                            },
                            "countryName": {
                              "type": "string"
                            }
                          },
                          "required": [ "locality", "region", "countryName" ],
                          "dependentRequired": {
                            "postOfficeBox": [ "streetAddress" ],
                            "extendedAddress": [ "streetAddress" ]
                          }
                        }
                        """;

        var input = """
                        {
                          "postOfficeBox": "123",
                          "streetAddress": "456 Main St",
                          "locality": "Cityville",
                          "region": "State",
                          "postalCode": "12345",
                          "countryName": "Country"
                        }
                        """;

        //Act
        var result = EndpointJsonSchemaTool.ValidateAndFilter(schemaJson: schemaJson, json: input);

        //Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.ValidatedJson.AsObject().ContainsKey("admin").Should().BeFalse();
    }
}
