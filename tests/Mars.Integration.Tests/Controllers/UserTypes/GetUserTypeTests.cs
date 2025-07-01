using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.UserTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.UserTypes;

/// <seealso cref="UserTypeController.Create(Shared.Contracts.UserTypes.CreateUserTypeRequest, CancellationToken)"/>
public sealed class GetUserTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/UserType";

    public GetUserTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserTypeController.Get);
        _ = nameof(UserTypeService.Get);
        var client = AppFixture.GetClient();

        var createdUserType = _fixture.Create<UserTypeEntity>();

        using var ef = AppFixture.MarsDbContext();
        await ef.UserTypes.AddAsync(createdUserType);
        await ef.SaveChangesAsync();

        var postTypeId = createdUserType.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(postTypeId).GetJsonAsync<UserTypeDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetUserType_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(UserTypeController.Get);
        _ = nameof(UserTypeService.Get);
        var client = AppFixture.GetClient();
        var invalidUserTypeId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidUserTypeId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
