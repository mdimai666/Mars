using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Roles;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Roles;

public class GetRoleTests : ApplicationTests
{
    const string _apiUrl = "/api/Role";

    public GetRoleTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetRole_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(RoleController.Get);
        _ = nameof(RoleService.Get);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task GetRole_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(RoleController.Get);
        _ = nameof(RoleService.Get);
        var client = AppFixture.GetClient();

        var createdRole = _fixture.Create<RoleEntity>();

        using var ef = AppFixture.MarsDbContext();
        await ef.Roles.AddAsync(createdRole);
        await ef.SaveChangesAsync();

        var postTypeId = createdRole.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(postTypeId).GetJsonAsync<RoleDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetRole_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(RoleController.Get);
        _ = nameof(RoleService.Get);
        var client = AppFixture.GetClient();
        var invalidRoleId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidRoleId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task ListRole_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(RoleController.List);
        _ = nameof(RoleService.List);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task ListRole_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(RoleController.List);
        _ = nameof(RoleService.List);
        var client = AppFixture.GetClient();

        var createdRoles = _fixture.CreateMany<RoleEntity>(3);

        using var ef = AppFixture.MarsDbContext();
        await ef.Roles.AddRangeAsync(createdRoles);
        await ef.SaveChangesAsync();

        var expectCount = ef.Roles.Count();

        //Act
        var result = await client.Request(_apiUrl).GetJsonAsync<ListDataResult<RoleSummaryResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(expectCount);
    }

    [IntegrationFact]
    public async Task ListRole_SearchRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(RoleController.List);
        _ = nameof(RoleService.List);
        var client = AppFixture.GetClient();

        var searchString = $"{Guid.NewGuid()}";
        var searchTitleString = $"nonUsePart_{searchString}";

        var createdRoles = _fixture.CreateMany<RoleEntity>(3);
        createdRoles.ElementAt(0).Name = searchTitleString;

        using var ef = AppFixture.MarsDbContext();
        await ef.Roles.AddRangeAsync(createdRoles);
        await ef.SaveChangesAsync();

        var expectRoleId = createdRoles.ElementAt(0).Id;

        var request = new ListRoleQueryRequest() { Search = searchString };

        //Act
        var result = await client.Request(_apiUrl)
                                    .AppendQueryParam(request)
                                    .GetJsonAsync<ListDataResult<RoleSummaryResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(1);
        result.Items.ElementAt(0).Id.Should().Be(expectRoleId);
    }

}
