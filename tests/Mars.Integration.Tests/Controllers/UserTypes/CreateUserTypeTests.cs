using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.UserTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.UserTypes;

/// <seealso cref="UserTypeController.Create(CreateUserTypeRequest, CancellationToken)"/>
public sealed class CreateUserTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/UserType";

    public CreateUserTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreateUserType_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(UserTypeController.Create);
        _ = nameof(UserTypeService.Create);
        var client = AppFixture.GetClient(true);

        var postTypeRequest = _fixture.Create<CreateUserTypeRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(postTypeRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CreateUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserTypeController.Create);
        _ = nameof(UserTypeService.Create);
        var client = AppFixture.GetClient();

        var postTypeRequest = _fixture.Create<CreateUserTypeRequest>();

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(postTypeRequest).CatchUserActionError();
        var result = await res.GetJsonAsync<UserTypeSummaryResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();
        using var ef = AppFixture.MarsDbContext();
        var postTypeEntity = ef.UserTypes.Include(s => s.MetaFields).FirstOrDefault(s => s.Id == postTypeRequest.Id);
        postTypeEntity.Should().NotBeNull();
        postTypeEntity.Should().BeEquivalentTo(postTypeRequest, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<CreateUserTypeRequest>()
            .Excluding(s => s.MetaFields)
            .ExcludingMissingMembers());
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
    public async Task CreateUserType_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(UserTypeController.Create);
        _ = nameof(UserTypeService.Create);
        var client = AppFixture.GetClient();

        var postTypeRequest = _fixture.Create<CreateUserTypeRequest>();
        postTypeRequest = postTypeRequest with
        {
            Title = string.Empty,
            TypeName = string.Empty,
        };

        var expectError = new Dictionary<string, string[]>()
        {
            [nameof(UserTypeSummaryResponse.Title)] = ["The Title field is required."],
            [nameof(UserTypeSummaryResponse.TypeName)] = ["The TypeName field is required.", "The field TypeName must be a string with a minimum length of 3 and a maximum length of 1000."],
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
