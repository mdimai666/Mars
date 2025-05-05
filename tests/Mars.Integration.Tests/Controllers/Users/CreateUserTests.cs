using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Repositories;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.Users;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.Users;

/// <seealso cref="Mars.Controllers.UserController"/>
public sealed class CreateUserTests : ApplicationTests
{
    const string _apiUrl = "/api/User";

    public CreateUserTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreateUser_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(UserController.Create);
        var client = AppFixture.GetClient(true);

        var userRequest = _fixture.Create<CreateUserRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(userRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CreateUser_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserRepository.Create);
        _ = nameof(UserController.Create);
        var client = AppFixture.GetClient();

        var user = _fixture.Create<CreateUserRequest>();

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(user).CatchUserActionError();
        var result = await res.GetJsonAsync<UserDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();
        using var ef = AppFixture.MarsDbContext();
        var dbUser = ef.Users.Include(s => s.Roles).FirstOrDefault(s => s.Email == user.Email);
        dbUser.Should().NotBeNull();
        dbUser.Should().BeEquivalentTo(user, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<CreateUserRequest>()
            .Excluding(f => f.Roles)
            .ExcludingMissingMembers());
        dbUser.Roles!.Select(s => s.Name).Should().BeEquivalentTo(user.Roles);
    }
}
