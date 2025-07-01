using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.UserTypes;

/// <seealso cref="UserTypeController.Delete(Guid, CancellationToken)"/>
public class DeleteUserTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/UserType";

    public DeleteUserTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeleteUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserTypeController.Delete);
        var client = AppFixture.GetClient();

        var postType = _fixture.Create<UserTypeEntity>();
        using var ef = AppFixture.MarsDbContext();
        await ef.UserTypes.AddAsync(postType);
        await ef.SaveChangesAsync();
        var deletingId = postType.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postTypeEntity = ef.UserTypes.FirstOrDefault(s => s.Id == deletingId);
        postTypeEntity.Should().BeNull();
    }

    [IntegrationFact]
    public async Task DeleteUserType_InvalidId_404NotFound()
    {
        //Arrange
        _ = nameof(UserTypeController.Delete);
        var client = AppFixture.GetClient();

        var deletingId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task DeleteUserType_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(UserTypeController.Delete);
        _ = nameof(UserTypeService.Delete);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
