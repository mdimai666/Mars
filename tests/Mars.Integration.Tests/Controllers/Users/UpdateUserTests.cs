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
using Mars.Shared.Contracts.Users;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.Users;

/// <seealso cref="Mars.Controllers.UserController"/>
public sealed class UpdateUserTests : ApplicationTests
{
    const string _apiUrl = "/api/User";

    public UpdateUserTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdateUser_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(UserController.Update);
        _ = nameof(UserRepository.Update);
        var client = AppFixture.GetClient(true);

        var userRequest = _fixture.Create<UpdateUserRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(userRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task UpdateUser_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserController.Update);
        _ = nameof(UserRepository.Update);
        var client = AppFixture.GetClient();

        var createdUser = _fixture.Create<UserEntity>();
        using var ef = AppFixture.MarsDbContext();
        ef.Users.Add(createdUser);
        var createdUserSecurityStamp = createdUser.SecurityStamp;
        ef.SaveChanges();

        var request = _fixture.Create<UpdateUserRequest>() with
        {
            Id = createdUser.Id,
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<UserDetailResponse>();

        //Assert
        result.Should().NotBeNull();

        ef.ChangeTracker.Clear();
        var dbUser = ef.Users.Include(s => s.Roles).FirstOrDefault(s => s.Id == request.Id);
        dbUser.Should().NotBeNull();

        dbUser.Should().BeEquivalentTo(request, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdateUserRequest>()
            .Excluding(f => f.Roles)
            .ExcludingMissingMembers());
        dbUser.Roles!.Select(s => s.Name).Should().BeEquivalentTo(request.Roles);
        dbUser.SecurityStamp.Should().NotBe(createdUserSecurityStamp);
    }

    [IntegrationFact]
    public async Task UpdateUser_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(UserController.Create);
        _ = nameof(UserService.Create);
        var client = AppFixture.GetClient();

        var updateUserRequest = _fixture.Create<UpdateUserRequest>();
        updateUserRequest = updateUserRequest with
        {
            FirstName = string.Empty,
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(updateUserRequest).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.ValidateSatisfy(new()
        {
            //[nameof(UpdateUserRequest.FirstName)] = ["*required*"],
            [nameof(UpdateUserRequest.FirstName)] = ["*обязательно*"],
        });
    }
}
