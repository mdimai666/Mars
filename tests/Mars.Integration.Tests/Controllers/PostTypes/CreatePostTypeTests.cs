using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.PostTypes;

/// <summary>
/// Post Type - Create API tests
/// </summary>
/// <seealso cref="PostTypeController.Create(CreatePostTypeRequest, CancellationToken)"/>
public sealed class CreatePostTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/PostType";

    public CreatePostTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreatePostType_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostTypeController.Create);
        _ = nameof(PostTypeService.Create);
        var client = AppFixture.GetClient(true);

        var postTypeRequest = _fixture.Create<CreatePostTypeRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(postTypeRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CreatePostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostTypeController.Create);
        _ = nameof(PostTypeService.Create);
        var client = AppFixture.GetClient();

        var postTypeRequest = _fixture.Create<CreatePostTypeRequest>();

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(postTypeRequest).CatchUserActionError();
        var result = await res.GetJsonAsync<PostTypeSummaryResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();
        var ef = AppFixture.MarsDbContext();
        var postTypeEntity = ef.PostTypes.Include(s => s.MetaFields)
                                         .Include(s => s.Statuses)
                                         .FirstOrDefault(s => s.Id == postTypeRequest.Id);
        postTypeEntity.Should().NotBeNull();
        postTypeEntity.Should().BeEquivalentTo(postTypeRequest, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<CreatePostTypeRequest>()
            .Excluding(s => s.PostStatusList)
            .Excluding(s => s.MetaFields)
            .ExcludingMissingMembers());
        postTypeEntity.Statuses.Should().AllSatisfy(e =>
        {
            var req = postTypeRequest.PostStatusList.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<CreatePostStatusRequest>()
                .ExcludingMissingMembers());
        });
        postTypeEntity.MetaFields.Should().AllSatisfy(e =>
        {
            var req = postTypeRequest.MetaFields.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<CreateMetaFieldRequest>()
                .Excluding(s => s.Variants)
                .ExcludingMissingMembers());

            e.Variants.Should().AllSatisfy(v =>
            {
                var va = req.Variants!.First(s => s.Id == v.Id);
                v.Should().BeEquivalentTo(va, options => options
                    .ComparingRecordsByValue()
                    .ComparingByMembers<CreateMetaFieldVariantRequest>()
                    .ExcludingMissingMembers());
            });
        });
    }

    [IntegrationFact]
    public async Task CreatePostType_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(PostTypeController.Create);
        _ = nameof(PostTypeService.Create);
        var client = AppFixture.GetClient();

        var postTypeRequest = _fixture.Create<CreatePostTypeRequest>();
        postTypeRequest = postTypeRequest with
        {
            Title = string.Empty,
            TypeName = string.Empty,
        };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(postTypeRequest).ReceiveValidationError();

        //Assert
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(PostTypeSummaryResponse.Title)] = ["The Title field is required."],
            [nameof(PostTypeSummaryResponse.TypeName)] = ["The TypeName field is required.", "The field TypeName must be a string with a minimum length of 3 and a maximum length of 1000."],
        });
    }

    [IntegrationFact]
    public async Task CreatePostType_WithMetafieldDuplicateKeyName_ShouldReturnValidationError()
    {
        //Arrange
        _ = nameof(PostTypeController.Create);
        _ = nameof(PostTypeService.Create);
        _ = nameof(MetaFieldsDuplicateQueryValidator);
        var client = AppFixture.GetClient();

        var postTypeRequest = _fixture.Create<CreatePostTypeRequest>();
        var metaFields = postTypeRequest.MetaFields.ToList();
        metaFields[1] = metaFields[1] with { Key = metaFields[0].Key };
        postTypeRequest = postTypeRequest with
        {
            MetaFields = metaFields
        };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(postTypeRequest).ReceiveValidationError();

        //Assert
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(CreatePostTypeRequest.MetaFields) + "[1].Key"] = [$"MetaField with key * дублируется*"],
        });
    }

    static JsonNode ListKindOptions()
        => new JsonObject { [MetaFieldKindCatalog.KindOption()] = MetaFieldKindCatalog.List };

    [IntegrationFact]
    public async Task CreatePostType_ListKindOnSingleRelationField_ShouldReturnValidationError()
    {
        //Arrange
        _ = nameof(PostTypeController.Create);
        _ = nameof(MetaFieldsDuplicateQueryValidator);
        var client = AppFixture.GetClient();

        var postTypeRequest = _fixture.Create<CreatePostTypeRequest>();
        var metaFields = postTypeRequest.MetaFields.ToList();
        metaFields[0] = metaFields[0] with
        {
            Type = MetaFieldType.Relation,
            ModelName = "Post.post",
            IsMultiple = false,
            Options = ListKindOptions(),
        };
        postTypeRequest = postTypeRequest with { MetaFields = metaFields };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(postTypeRequest).ReceiveValidationError();

        //Assert
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(CreatePostTypeRequest.MetaFields) + "[0].Options"] = [$"*несколькими значениями*"],
        });
    }

    [IntegrationFact]
    public async Task CreatePostType_ListKindOnMultipleRelationField_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostTypeController.Create);
        _ = nameof(PostTypeService.Create);
        var client = AppFixture.GetClient();

        var postTypeRequest = _fixture.Create<CreatePostTypeRequest>();
        var metaFields = postTypeRequest.MetaFields.ToList();
        metaFields[0] = metaFields[0] with
        {
            Type = MetaFieldType.Relation,
            ModelName = "Post.post",
            IsMultiple = true,
            Options = ListKindOptions(),
        };
        postTypeRequest = postTypeRequest with { MetaFields = metaFields };

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(postTypeRequest).CatchUserActionError();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        var ef = AppFixture.MarsDbContext();
        var created = ef.PostTypes.Include(s => s.MetaFields).FirstOrDefault(s => s.Id == postTypeRequest.Id);
        created.Should().NotBeNull();
        var createdField = created!.MetaFields.Single(s => s.Id == metaFields[0].Id);
        createdField.IsMultiple.Should().BeTrue();
        createdField.Options.GetKind().Should().Be(MetaFieldKindCatalog.List);
    }
}
