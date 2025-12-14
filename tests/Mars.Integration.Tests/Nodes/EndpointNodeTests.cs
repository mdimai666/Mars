using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Nodes;

public class EndpointNodeTests : ApplicationTests
{
    const string _endpointUrl = "/api2/endpoint1";
    private readonly INodeService _nodeService;

    public EndpointNodeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
    }

    void SetupNodes(EndpointNode endpointNode)
    {
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(endpointNode)
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);
    }

    [IntegrationFact]
    public async Task ValidateRequestRequirements_IsRequireAuthorize_ShouldStatus401()
    {
        //Arrange
        _ = nameof(EndpointNodeImpl.Execute);
        var client = AppFixture.GetClient(true);
        SetupNodes(new EndpointNode
        {
            Method = "POST",
            UrlPattern = _endpointUrl,
            EndpointInputModel = EndpointInputModelType.JsonSchema,
            IsRequireAuthorize = true
        });

        //Act
        var result = await client.Request(_endpointUrl).AllowAnyHttpStatus().PostJsonAsync(new { });

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task ValidateRequestRequirements_AllowedRoles_ShouldStatus403()
    {
        //Arrange
        _ = nameof(EndpointNodeImpl.Execute);
        var client = AppFixture.GetClient();
        SetupNodes(new EndpointNode
        {
            Method = "POST",
            UrlPattern = _endpointUrl,
            EndpointInputModel = EndpointInputModelType.JsonSchema,
            IsRequireAuthorize = true,
            AllowedRoles = ["Superuser"]
        });

        //Act
        var result = await client.Request(_endpointUrl).AllowAnyHttpStatus().PostJsonAsync(new { });

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [IntegrationFact]
    public async Task JsonSchema_InvalidJsonBody_ShouldStatus400()
    {
        //Arrange
        _ = nameof(EndpointNodeImpl.Execute);
        var client = AppFixture.GetClient();
        SetupNodes(new EndpointNode
        {
            Method = "POST",
            UrlPattern = _endpointUrl,
            EndpointInputModel = EndpointInputModelType.JsonSchema,
            IsRequireAuthorize = true,
        });

        //Act
        var result = await client.Request(_endpointUrl).AllowAnyHttpStatus().PostStringAsync("xx");

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var detail = await result.GetJsonAsync<ValidationProblemDetails>();
        detail.Errors.First().Value.First().Should().Contain("RequestBodyIsNotValidJson");
    }

    public const string PersonJsonSchema = """
            {
              "type": "object",
              "properties": {
                "first_name": { "type": "string" },
                "last_name": { "type": "string" },
                "birthday": { "type": "string", "format": "date" },
                "address": {
                   "type": "object",
                   "properties": {
                     "street_address": { "type": "string" },
                     "city": { "type": "string" },
                     "state": { "type": "string" },
                     "country": { "type" : "string" }
                   }
                }
              },
              "required": [ "first_name", "last_name" ],
            }
            """;

    [IntegrationFact]
    public async Task JsonSchema_ValidJson_ShouldSuccess()
    {
        //Arrange
        _ = nameof(EndpointNodeImpl.Execute);
        var client = AppFixture.GetClient();
        var exampleObject = new
        {
            first_name = "John",
            last_name = "Doe",
            birthday = "1990-01-01",
            address = new
            {
                street_address = "123 Main St",
                city = "Anytown",
                state = "CA",
                country = "USA"
            }
        };
        var person = new PersonClass()
        {
            First_name = "John",
            Last_name = "Doe",
            Birthday = new DateTime(1990, 1, 1),
            Address = new PersonClass.AddressClass()
            {
                Street_address = "123 Main St",
                City = "Anytown",
                State = "CA",
                Country = "USA"
            }
        };

        SetupNodes(new EndpointNode
        {
            Method = "POST",
            UrlPattern = _endpointUrl,
            EndpointInputModel = EndpointInputModelType.JsonSchema,
            IsRequireAuthorize = true,
            JsonSchema = PersonJsonSchema
        });

        //Act
        var res = await client.Request(_endpointUrl).AllowAnyHttpStatus().PostJsonAsync(exampleObject);

        //Assert
        if (res.StatusCode == StatusCodes.Status400BadRequest)
        {
            var errors = await res.GetJsonAsync<ValidationProblemDetails>();
            res.StatusCode.Should().Be(StatusCodes.Status200OK);
        }
        var result = await res.GetJsonAsync<PersonClass>();
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(person);
    }

    public class PersonClass
    {
        public string First_name { get; set; } = default!;
        public string Last_name { get; set; } = default!;
        public DateTime Birthday { get; set; } = default!;
        public AddressClass Address { get; set; } = default!;

        public class AddressClass
        {
            public string Street_address { get; set; } = "";
            public string City { get; set; } = "";
            public string State { get; set; } = "";
            public string Country { get; set; } = "";
        };
    }

    [IntegrationFact]
    public async Task JsonSchema_ValidationError_FailFirstNameIsRequired()
    {
        //Arrange
        _ = nameof(EndpointNodeImpl.Execute);
        var client = AppFixture.GetClient();
        var exampleObject = new
        {
            last_name = "Doe",
            birthday = "1990-01-01",
            address = new
            {
                street_address = "123 Main St",
                city = "Anytown",
                state = "CA",
                country = "USA"
            }
        };
        SetupNodes(new EndpointNode
        {
            Method = "POST",
            UrlPattern = _endpointUrl,
            EndpointInputModel = EndpointInputModelType.JsonSchema,
            IsRequireAuthorize = true,
            JsonSchema = PersonJsonSchema
        });

        //Act
        var result = await client.Request(_endpointUrl).PostJsonAsync(exampleObject).ReceiveValidationError();

        //Assert
        result.Errors.First().Value.First().Should().Match("*first_name*: required");
    }
}
