using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.NavMenus;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.NavMenus;

/// <seealso cref="Mars.Controllers.NavMenuController"/>
public class CreateNavMenuTests : ApplicationTests
{
    const string _apiUrl = "/api/NavMenu";

    public CreateNavMenuTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreateNavMenu_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(NavMenuController.Create);
        _ = nameof(NavMenuRepository.Create);
        var client = AppFixture.GetClient(true);

        var request = _fixture.Create<CreateNavMenuRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CreateNavMenu_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(NavMenuController.Create);
        _ = nameof(NavMenuRepository.Create);
        var client = AppFixture.GetClient();

        var request = _fixture.Create<CreateNavMenuRequest>();

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(request).CatchUserActionError();
        var resultId = await res.GetJsonAsync<Guid>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        resultId.Should().NotBeEmpty();
        var ef = AppFixture.MarsDbContext();
        var dbNavMenu = ef.NavMenus.AsNoTracking().FirstOrDefault(s => s.Id == resultId);
        dbNavMenu.Should().NotBeNull();
        dbNavMenu.Title.Should().Be(request.Title);
    }

    [IntegrationFact]
    public async Task CreateNavMenu_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(NavMenuController.Create);
        _ = nameof(NavMenuService.Create);
        var client = AppFixture.GetClient();

        var request = _fixture.Create<CreateNavMenuRequest>();
        request = request with
        {
            Title = string.Empty,
        };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(request).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(NavMenuDetailResponse.Title)] = ["*Title*required*"],
        });
    }

}
