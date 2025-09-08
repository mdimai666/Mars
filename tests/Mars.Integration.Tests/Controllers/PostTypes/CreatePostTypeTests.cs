using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
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
        using var ef = AppFixture.MarsDbContext();
        var postTypeEntity = ef.PostTypes.Include(s => s.MetaFields).FirstOrDefault(s => s.Id == postTypeRequest.Id);
        postTypeEntity.Should().NotBeNull();
        postTypeEntity.Should().BeEquivalentTo(postTypeRequest, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<CreatePostTypeRequest>()
            .WithMapping(nameof(PostTypeEntity.PostContentType), nameof(CreatePostTypeRequest.PostContentSettings))
            .Excluding(s => s.PostStatusList)
            .Excluding(s => s.MetaFields)
            .ExcludingMissingMembers());
        postTypeEntity.PostStatusList.Should().AllSatisfy(e =>
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

        var expectError = new Dictionary<string, string[]>()
        {
            [nameof(PostTypeSummaryResponse.Title)] = ["The Title field is required."],
            [nameof(PostTypeSummaryResponse.TypeName)] = ["The TypeName field is required.", "The field TypeName must be a string with a minimum length of 3 and a maximum length of 1000."],
        };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(postTypeRequest).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.Should().HaveSameCount(expectError);
        result.Errors.Should().AllSatisfy(x =>
        {
            expectError[x.Key].Should().BeEquivalentTo(x.Value); //order insensetive

        });
    }
}
