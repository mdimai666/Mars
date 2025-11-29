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

namespace Mars.Integration.Tests.Controllers.NavMenus;

/// <seealso cref="NavMenuController.Delete(Guid, CancellationToken)"/>
public class DeleteNavMenuTests : ApplicationTests
{
    const string _apiUrl = "/api/NavMenu";

    public DeleteNavMenuTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeleteNavMenu_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(NavMenuController.Delete);
        var client = AppFixture.GetClient();

        var post = _fixture.Create<NavMenuEntity>();
        var ef = AppFixture.MarsDbContext();
        await ef.NavMenus.AddAsync(post);
        await ef.SaveChangesAsync();
        var deletingId = post.Id;

        //Act
        var result = await client.Request(_apiUrl, deletingId).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var menuEntity = ef.NavMenus.FirstOrDefault(s => s.Id == deletingId);
        menuEntity.Should().BeNull();
    }

    [IntegrationFact]
    public async Task DeleteNavMenu_InvalidId_404NotFound()
    {
        //Arrange
        _ = nameof(NavMenuController.Delete);
        var client = AppFixture.GetClient();
        var deletingId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl, deletingId).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task DeleteNavMenu_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(NavMenuController.Delete);
        _ = nameof(NavMenuService.Delete);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
