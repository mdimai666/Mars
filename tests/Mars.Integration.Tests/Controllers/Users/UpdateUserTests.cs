using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Contracts.MetaFields;
using Mars.Core.Utils;
using Mars.Data.Entities;
using Mars.Data.Repositories;
using Mars.Identity.Contracts.Auth;
using Mars.Identity.Contracts.Users;
using Mars.Identity.Host.Controllers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.Users;

/// <seealso cref="Mars.Identity.Host.Controllers.UserController"/>
public sealed class UpdateUserTests : ApplicationTests
{
    const string _apiUrl = "/api/User";

    public UpdateUserTests(ApplicationFixture appFixture) : base(appFixture)
    {
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
    public async Task UpdateUser_ValidRequest_Succeeds()
    {
        //Arrange
        _ = nameof(UserController.Update);
        _ = nameof(UserRepository.Update);
        var client = AppFixture.GetClient();

        var createdUser = _fixture.Create<UserEntity>();
        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToArray();
        var userType = ef.UserTypes.Include(s => s.MetaFields).First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        userType.MetaFields = metaFields.ToList();
        var metaValues = metaFields.Select(mf =>
        {
            var mv = _fixture.MetaValueEntity<UserMetaValueEntity>(mf.Id, mf.Type);
            mv.MetaField = mf;
            return mv;
        }).ToList();
        createdUser.MetaValues = metaValues;
        ef.Users.Add(createdUser);
        var newRole = _fixture.Create<RoleEntity>();
        ef.Roles.Add(newRole);
        var createdUserSecurityStamp = createdUser.SecurityStamp;
        ef.SaveChanges();

        var metaValueUpdates = metaValues.Select((mv, i) => _fixture.UpdateSimpleCreateMetaValueRequest(i != 0 ? mv.Id : Guid.NewGuid(), mv.MetaField.Id, mv.MetaField.Type)).ToArray();

        var request = _fixture.Create<UpdateUserRequest>() with
        {
            Id = createdUser.Id,
            Type = userType.TypeName,
            MetaValues = metaValueUpdates,
            Roles = [newRole.Name!]
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<UserDetailResponse>();

        //Assert
        result.Should().NotBeNull();

        ef.ChangeTracker.Clear();
        var dbUser = ef.Users.Include(s => s.Roles)
                                .Include(s => s.MetaValues)
                                .FirstOrDefault(s => s.Id == request.Id);
        dbUser.Should().NotBeNull();

        dbUser.Should().BeEquivalentTo(request, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdateUserRequest>()
            .Excluding(f => f.Roles)
            .Excluding(s => s.MetaValues)
            .ExcludingMissingMembers());
        dbUser.Roles!.Select(s => s.Name).Should().BeEquivalentTo(request.Roles);
        dbUser.SecurityStamp.Should().NotBe(createdUserSecurityStamp);

        dbUser.MetaValues.Should().AllSatisfy(e =>
        {
            var req = request.MetaValues.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<UpdateMetaValueRequest>()
                //.Excluding(s => s.DateTime)
                .ExcludingMissingMembers());
            //e.DateTime.Date.ToString("g").Should().Be(req.DateTime.Date.ToString("g"));

        });
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

    [IntegrationFact]
    public async Task UpdateUser_ChangeUserNameToInvalid_ValidateError()
    {
        //Arrange — смена имени на неформатное (почта/пробелы) валидируется строго
        _ = nameof(UserController.Update);
        var client = AppFixture.GetClient();
        var ef = AppFixture.MarsDbContext();

        var createdUser = _fixture.Create<UserEntity>();
        createdUser.MetaValues = [];
        createdUser.UserTypeId = ef.UserTypes.First(s => s.TypeName == UserTypeEntity.DefaultTypeName).Id;
        ef.Users.Add(createdUser);
        ef.SaveChanges();
        var roleName = ef.Roles.AsNoTracking().First().Name!;

        var request = _fixture.Create<UpdateUserRequest>() with
        {
            Id = createdUser.Id,
            Type = UserTypeEntity.DefaultTypeName,
            MetaValues = [],
            Roles = [roleName],
            UserName = "some@mail.localhost",
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.Should().ContainKey(nameof(UpdateUserRequest.UserName));
        result.Errors[nameof(UpdateUserRequest.UserName)].Should().ContainMatch("*can only contain letters*");
    }

    [IntegrationFact]
    public async Task UpdateUser_Rename_NewNameWorksForLogin()
    {
        //Arrange
        _ = nameof(UserController.Update);
        var client = AppFixture.GetClient();
        var ef = AppFixture.MarsDbContext();
        var roleName = ef.Roles.AsNoTracking().First().Name!;
        var password = Password.Generate(10, 4);

        var createRequest = _fixture.Create<CreateUserRequest>() with
        {
            Type = UserTypeEntity.DefaultTypeName,
            Roles = [roleName],
            Password = password,
            MetaValues = [],
        };
        var created = await client.Request(_apiUrl).PostJsonAsync(createRequest).CatchUserActionError().ReceiveJson<UserDetailResponse>();
        created.Should().NotBeNull();

        var newName = "renamed" + Guid.NewGuid().ToString("N")[..8];
        var updateRequest = new UpdateUserRequest
        {
            Id = created.Id,
            UserName = newName,
            FirstName = created.FirstName,
            LastName = created.LastName,
            MiddleName = created.MiddleName,
            Email = created.Email,
            Roles = [roleName],
            PhoneNumber = created.PhoneNumber,
            BirthDate = created.BirthDate,
            Gender = created.Gender,
            AvatarUrl = created.AvatarUrl,
            Type = created.Type,
            MetaValues = [],
        };

        //Act
        var updated = await client.Request(_apiUrl).PutJsonAsync(updateRequest).CatchUserActionError().ReceiveJson<UserDetailResponse>();

        //Assert — rename сохраняется вместе с NormalizedUserName
        updated.UserName.Should().Be(newName);
        ef.ChangeTracker.Clear();
        var entity = ef.Users.AsNoTracking().First(s => s.Id == created.Id);
        entity.NormalizedUserName.Should().Be(newName.ToUpper());

        // вход: новое имя — успешно, старое — нет, почта — успешно
        var anon = AppFixture.GetClient(true);

        var loginNew = await anon.Request("/api/Account", "Login")
            .PostJsonAsync(new AuthCredentialsRequest { Login = newName, Password = password })
            .ReceiveJson<AuthResultResponse>();
        loginNew.IsAuthSuccessful.Should().BeTrue();

        var loginOld = await anon.Request("/api/Account", "Login")
            .AllowAnyHttpStatus()
            .PostJsonAsync(new AuthCredentialsRequest { Login = createRequest.UserName, Password = password });
        loginOld.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var loginEmail = await anon.Request("/api/Account", "Login")
            .PostJsonAsync(new AuthCredentialsRequest { Login = createRequest.Email!, Password = password })
            .ReceiveJson<AuthResultResponse>();
        loginEmail.IsAuthSuccessful.Should().BeTrue();
    }

}
