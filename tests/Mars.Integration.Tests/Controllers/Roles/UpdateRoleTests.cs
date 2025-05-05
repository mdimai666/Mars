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
using Mars.Shared.Contracts.Roles;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Roles;

/// <seealso cref="Mars.Controllers.RoleController"/>
public class UpdateRoleTests : ApplicationTests
{
    const string _apiUrl = "/api/Role";

    public UpdateRoleTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdateRole_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(RoleController.Create);
        _ = nameof(RoleRepository.Update);
        var client = AppFixture.GetClient(true);

        var feedbackRequest = _fixture.Create<UpdateRoleRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(feedbackRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task UpdateRole_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(RoleController.Update);
        _ = nameof(RoleRepository.Update);
        var client = AppFixture.GetClient();

        var createdRole = _fixture.Create<RoleEntity>();
        using var ef = AppFixture.MarsDbContext();
        ef.Roles.Add(createdRole);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var post = new UpdateRoleRequest
        {
            Id = createdRole.Id,
            Name = "GodRole",
        };

        //Act
        var res = await client.Request(_apiUrl).PutJsonAsync(post).CatchUserActionError();
        var result = await res.GetJsonAsync<RoleDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeNull();

        ef.ChangeTracker.Clear();
        var dbRole = ef.Roles.FirstOrDefault(s => s.Id == post.Id);
        dbRole.Should().NotBeNull();

        dbRole.Should().BeEquivalentTo(post, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdateRoleRequest>()
            .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public async Task UpdateRole_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(RoleController.Update);
        _ = nameof(RoleService.Update);
        var client = AppFixture.GetClient();

        var updateRoleRequest = _fixture.Create<UpdateRoleRequest>();
        updateRoleRequest = updateRoleRequest with
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(updateRoleRequest).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(UpdateRoleRequest.Name)] = ["*Name*required*"],
        });
    }
}
