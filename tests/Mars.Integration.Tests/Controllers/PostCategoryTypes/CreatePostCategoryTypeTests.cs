using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.PostCategoryTypes;

/// <seealso cref="PostCategoryTypeController.Create(CreatePostCategoryTypeRequest, CancellationToken)"/>
public sealed class CreatePostCategoryTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/PostCategoryType";

    public CreatePostCategoryTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreatePostCategoryType_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Create);
        _ = nameof(PostCategoryTypeService.Create);
        var client = AppFixture.GetClient(true);

        var postCategoryTypeRequest = _fixture.Create<CreatePostCategoryTypeRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(postCategoryTypeRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CreatePostCategoryType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Create);
        _ = nameof(PostCategoryTypeService.Create);
        var client = AppFixture.GetClient();

        var postCategoryTypeRequest = _fixture.Create<CreatePostCategoryTypeRequest>();

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(postCategoryTypeRequest).CatchUserActionError();
        var result = await res.GetJsonAsync<PostCategoryTypeSummaryResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();
        var ef = AppFixture.MarsDbContext();
        var postCategoryTypeEntity = ef.PostCategoryTypes.Include(s => s.MetaFields).FirstOrDefault(s => s.Id == postCategoryTypeRequest.Id);
        postCategoryTypeEntity.Should().NotBeNull();
        postCategoryTypeEntity.Should().BeEquivalentTo(postCategoryTypeRequest, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<CreatePostCategoryTypeRequest>()
            .Excluding(s => s.MetaFields)
            .ExcludingMissingMembers());
        postCategoryTypeEntity.MetaFields.Should().AllSatisfy(e =>
        {
            var req = postCategoryTypeRequest.MetaFields.First(s => s.Id == e.Id);
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
    public async Task CreatePostCategoryType_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Create);
        _ = nameof(PostCategoryTypeService.Create);
        var client = AppFixture.GetClient();

        var postCategoryTypeRequest = _fixture.Create<CreatePostCategoryTypeRequest>();
        postCategoryTypeRequest = postCategoryTypeRequest with
        {
            Title = string.Empty,
            TypeName = string.Empty,
        };

        var expectError = new Dictionary<string, string[]>()
        {
            [nameof(PostCategoryTypeSummaryResponse.Title)] = ["The Title field is required."],
            [nameof(PostCategoryTypeSummaryResponse.TypeName)] = ["The TypeName field is required.", "The field TypeName must be a string with a minimum length of 3 and a maximum length of 1000."],
        };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(postCategoryTypeRequest).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.Should().HaveSameCount(expectError);
        result.Errors.Should().AllSatisfy(x =>
        {
            expectError[x.Key].Should().BeEquivalentTo(x.Value); //order insensetive

        });
    }
}
