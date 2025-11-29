using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.NavMenus;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.NavMenus;

/// <seealso cref="Mars.Controllers.NavMenuController"/>
public class UpdateNavMenuTests : ApplicationTests
{
    const string _apiUrl = "/api/NavMenu";

    public UpdateNavMenuTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdateNavMenu_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(NavMenuController.Create);
        _ = nameof(NavMenuRepository.Update);
        var client = AppFixture.GetClient(true);

        var feedbackRequest = _fixture.Create<UpdateNavMenuRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(feedbackRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task UpdateNavMenu_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(NavMenuController.Update);
        _ = nameof(NavMenuRepository.Update);
        var client = AppFixture.GetClient();

        var createdNavMenu = _fixture.Create<NavMenuEntity>();
        var ef = AppFixture.MarsDbContext();
        ef.NavMenus.Add(createdNavMenu);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var post = _fixture.Create<UpdateNavMenuRequest>() with
        {
            Id = createdNavMenu.Id,
            Title = "New NavMenu",
        };

        //Act
        var res = await client.Request(_apiUrl).PutJsonAsync(post).CatchUserActionError();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);

        ef.ChangeTracker.Clear();
        var dbNavMenu = ef.NavMenus.FirstOrDefault(s => s.Id == post.Id);
        dbNavMenu.Should().NotBeNull();

        dbNavMenu.Should().BeEquivalentTo(post, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdateNavMenuRequest>()
            .Excluding(s => s.MenuItems)
            .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public async Task UpdateNavMenu_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(NavMenuController.Update);
        _ = nameof(NavMenuService.Update);
        var client = AppFixture.GetClient();

        var updateNavMenuRequest = _fixture.Create<UpdateNavMenuRequest>();
        updateNavMenuRequest = updateNavMenuRequest with
        {
            Id = Guid.NewGuid(),
            Title = string.Empty,
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(updateNavMenuRequest).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(UpdateNavMenuRequest.Title)] = ["*Title*required*"],
        });
    }
}
