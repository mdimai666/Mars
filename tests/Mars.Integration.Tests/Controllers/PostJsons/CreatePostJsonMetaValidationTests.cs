using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.PostJsons;
using Mars.Cms.Host.Controllers;
using Mars.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PostJsons;

/// <summary>Валидация мета-значений (Options.validators) на пути записи JSON</summary>
public class CreatePostJsonMetaValidationTests : ApplicationTests
{
    const string _apiUrl = "/api/PostJson";

    public CreatePostJsonMetaValidationTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreatePostJson_MetaValueAgainstValidator_ShouldRejectInvalidAndAcceptValid()
    {
        //Arrange
        _ = nameof(PostJsonController.Create);
        _ = nameof(MetaValuesValidator);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var codeKey = $"code_{Guid.NewGuid():N}"[..12];
        var field = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Code",
            Key = codeKey,
            Type = EMetaFieldType.String,
            CreatedAt = DateTimeOffset.Now,
            Options = new JsonObject
            {
                ["validators"] = new JsonArray(new JsonObject
                {
                    ["type"] = "regex",
                    ["params"] = new JsonObject { ["pattern"] = @"^\d{3}$", ["message"] = "три цифры" },
                }),
            },
        };

        var postType = await ef.PostTypes.Include(s => s.MetaFields).FirstAsync(s => s.TypeName == "post");
        postType.MetaFields = [.. postType.MetaFields, field];
        await ef.MetaFields.AddAsync(field);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        //Act: значение не проходит регулярку — 400
        var invalid = _fixture.Create<CreatePostJsonRequest>() with
        {
            Type = "post",
            Meta = new Dictionary<string, JsonNode> { [codeKey] = "12" },
        };
        var invalidResult = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(invalid);

        //Assert
        invalidResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        //Act: валидное значение — 200
        var valid = _fixture.Create<CreatePostJsonRequest>() with
        {
            Type = "post",
            Meta = new Dictionary<string, JsonNode> { [codeKey] = "123" },
        };
        var validResult = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(valid);

        //Assert
        validResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }
}
