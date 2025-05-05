using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.Roles;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.Roles;

/// <seealso cref="Mars.Controllers.RoleController"/>
public class CreateRoleTests : ApplicationTests
{
    const string _apiUrl = "/api/Role";

    public CreateRoleTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreateRole_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(RoleController.Create);
        _ = nameof(RoleRepository.Create);
        var client = AppFixture.GetClient(true);

        var request = _fixture.Create<CreateRoleRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CreateRole_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(RoleController.Create);
        _ = nameof(RoleRepository.Create);
        var client = AppFixture.GetClient();

        var request = _fixture.Create<CreateRoleRequest>();

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(request).CatchUserActionError();
        var result = await res.GetJsonAsync<RoleDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();
        using var ef = AppFixture.MarsDbContext();
        var dbRole = ef.Roles.AsNoTracking().FirstOrDefault(s => s.Id == result.Id);
        dbRole.Should().NotBeNull();
        dbRole.Name.Should().Be(request.Name);
    }

    [IntegrationFact]
    public async Task CreateRole_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(RoleController.Create);
        _ = nameof(RoleService.Create);
        var client = AppFixture.GetClient();

        var request = _fixture.Create<CreateRoleRequest>();
        request = request with
        {
            Name = string.Empty,
        };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(request).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(RoleDetailResponse.Name)] = ["*Name*required*"],
        });
    }

}
