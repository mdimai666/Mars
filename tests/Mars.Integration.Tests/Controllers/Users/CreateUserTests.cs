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
using Mars.Shared.Contracts.MetaFields;
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

        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var userType = ef.UserTypes.Include(s => s.MetaFields).First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        userType.MetaFields = metaFields;
        ef.MetaFields.AddRange(metaFields);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var userRequest = _fixture.Create<CreateUserRequest>() with
        {
            MetaValues = metaFields.Select(mf => _fixture.CreateSimpleCreateMetaValueRequest(mf.Id, mf.Type)).ToArray(),
        };

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(userRequest).CatchUserActionError();
        var result = await res.GetJsonAsync<UserDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();
        var dbUser = ef.Users.Include(s => s.Roles)
                                .Include(s => s.MetaValues)
                                .FirstOrDefault(s => s.Email == userRequest.Email);
        dbUser.Should().NotBeNull();
        dbUser.Should().BeEquivalentTo(userRequest, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<CreateUserRequest>()
            .Excluding(f => f.Roles)
            .Excluding(s => s.MetaValues)
            .ExcludingMissingMembers());
        dbUser.Roles!.Select(s => s.Name).Should().BeEquivalentTo(userRequest.Roles);

        dbUser.MetaValues.Should().AllSatisfy(e =>
        {
            var req = userRequest.MetaValues.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<CreateMetaValueRequest>()
                //.Excluding(s => s.DateTime)
                .ExcludingMissingMembers());
            //e.DateTime.Date.ToString("g").Should().Be(req.DateTime.Date.ToString("g"));

        });
    }

    [IntegrationFact]
    public async Task CreateUser_UsernameInvalidByBlacklist_ShouldFail()
    {
        //Arrange
        _ = nameof(UserService.Create);
        _ = nameof(UserController.Create);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var userType = ef.UserTypes.AsNoTracking().Include(s => s.MetaFields).First(s => s.TypeName == UserTypeEntity.DefaultTypeName);

        var userRequest = _fixture.Create<CreateUserRequest>() with
        {
            UserName = "sex"
        };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(userRequest).ReceiveValidationError();

        //Assert
        result.Status.Should().Be(StatusCodes.Status400BadRequest);
        result.Errors.Should().HaveCount(1);
        result.Errors[nameof(CreateUserRequest.UserName)].Should().ContainMatch("This username is reserved or not allowed.");

    }
}
