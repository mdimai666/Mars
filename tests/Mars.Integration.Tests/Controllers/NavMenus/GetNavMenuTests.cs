using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.NavMenus;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.NavMenus;

public class GetNavMenuTests : ApplicationTests
{
    const string _apiUrl = "/api/NavMenu";

    public GetNavMenuTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetNavMenu_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(NavMenuController.Get);
        _ = nameof(NavMenuService.Get);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task GetNavMenu_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(NavMenuController.Get);
        _ = nameof(NavMenuService.Get);
        var client = AppFixture.GetClient();

        var createdNavMenu = _fixture.Create<NavMenuEntity>();

        using var ef = AppFixture.MarsDbContext();
        await ef.NavMenus.AddAsync(createdNavMenu);
        await ef.SaveChangesAsync();

        var manuId = createdNavMenu.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(manuId).GetJsonAsync<NavMenuDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetNavMenu_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(NavMenuController.Get);
        _ = nameof(NavMenuService.Get);
        var client = AppFixture.GetClient();
        var invalidNavMenuId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidNavMenuId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task ListNavMenu_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(NavMenuController.List);
        _ = nameof(NavMenuService.List);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task ListNavMenu_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(NavMenuController.List);
        _ = nameof(NavMenuService.List);
        var client = AppFixture.GetClient();

        var createdNavMenus = _fixture.CreateMany<NavMenuEntity>(3);

        using var ef = AppFixture.MarsDbContext();
        await ef.NavMenus.AddRangeAsync(createdNavMenus);
        await ef.SaveChangesAsync();

        var expectCount = ef.NavMenus.Count();

        //Act
        var result = await client.Request(_apiUrl).GetJsonAsync<ListDataResult<NavMenuSummaryResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(expectCount);
    }

    [IntegrationFact]
    public async Task ListNavMenu_SearchRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(NavMenuController.List);
        _ = nameof(NavMenuService.List);
        var client = AppFixture.GetClient();

        var searchString = $"{Guid.NewGuid()}";
        var searchTitleString = $"nonUsePart_{searchString}";

        var createdNavMenus = _fixture.CreateMany<NavMenuEntity>(3);
        createdNavMenus.ElementAt(0).Title = searchTitleString;

        using var ef = AppFixture.MarsDbContext();
        await ef.NavMenus.AddRangeAsync(createdNavMenus);
        await ef.SaveChangesAsync();

        var expectNavMenuId = createdNavMenus.ElementAt(0).Id;

        var request = new ListNavMenuQueryRequest() { Search = searchString };

        //Act
        var result = await client.Request(_apiUrl)
                                    .AppendQueryParam(request)
                                    .GetJsonAsync<ListDataResult<NavMenuSummaryResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(1);
        result.Items.ElementAt(0).Id.Should().Be(expectNavMenuId);
    }

}
