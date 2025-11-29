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

namespace Mars.Integration.Tests.Controllers.Users;

/// <seealso cref="UserController.Delete(Guid, CancellationToken)"/>
public class DeleteUserTests : ApplicationTests
{
    const string _apiUrl = "/api/User";

    public DeleteUserTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeleteUser_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserController.Delete);
        var client = AppFixture.GetClient();

        var user = _fixture.Create<UserEntity>();
        var ef = AppFixture.MarsDbContext();
        await ef.Users.AddAsync(user);
        await ef.SaveChangesAsync();
        var deletingId = user.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var userEntity = ef.Users.FirstOrDefault(s => s.Id == deletingId);
        userEntity.Should().BeNull();
    }

    [IntegrationFact]
    public async Task DeleteUser_InvalidId_404NotFound()
    {
        //Arrange
        _ = nameof(UserController.Delete);
        var client = AppFixture.GetClient();
        var deletingId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task DeleteUser_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(UserController.Delete);
        _ = nameof(UserService.Delete);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
