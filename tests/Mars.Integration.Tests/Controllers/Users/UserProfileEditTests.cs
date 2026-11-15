using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Data.Entities;
using Mars.Identity.Contracts.Users;
using Mars.Identity.Contracts.Users.UserProfiles;
using Mars.Identity.Host.Controllers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.Users;

/// <seealso cref="AccountController"/>
public sealed class UserProfileEditTests : ApplicationTests
{
    const string _apiUrl = "/api/Account";

    public UserProfileEditTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task GetProfileEditModel_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(AccountController.ProfileEditModel);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, "Profile", "EditModel").AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task GetProfileEditModel_ValidRequest_Succeeds()
    {
        //Arrange
        _ = nameof(AccountController.ProfileEditModel);
        var client = AppFixture.GetClient();

        //Act
        var result = await client.Request(_apiUrl, "Profile", "EditModel").GetJsonAsync<UserProfileEditViewModel>();

        //Assert
        result.Should().NotBeNull();
        result.User.UserName.Should().Be(UserConstants.TestUserUsername);
        result.User.Email.Should().Be(UserConstants.TestUserEmail);
        result.User.Roles.Should().BeEquivalentTo(UserConstants.TestUser.Roles);
        result.UserType.TypeName.Should().Be(UserConstants.TestUser.Type);
        result.Form.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task UpdateProfile_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(AccountController.UpdateProfile);
        var client = AppFixture.GetClient(true);

        var request = CreateValidRequest();

        //Act
        var result = await client.Request(_apiUrl, "Profile").AllowAnyHttpStatus().PutJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task UpdateProfile_ValidRequest_UpdatesWhitelistedFieldsOnly()
    {
        //Arrange
        _ = nameof(AccountController.UpdateProfile);
        var ef = AppFixture.MarsDbContext();

        var userType = ef.UserTypes.First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        var role = ef.Roles.AsNoTracking().First();
        var createdUser = _fixture.Create<UserEntity>();
        createdUser.MetaValues = [];
        createdUser.UserTypeId = userType.Id;
        ef.Users.Add(createdUser);
        ef.UserRoles.Add(new UserRoleEntity { UserId = createdUser.Id, RoleId = role.Id });
        ef.SaveChanges();

        var userNameBefore = createdUser.UserName;
        var emailBefore = createdUser.Email;
        var typeBefore = userType.TypeName;

        var client = AppFixture.GetClientForUser(createdUser.Id);

        var request = CreateValidRequest();

        //Act
        var result = await client.Request(_apiUrl, "Profile")
                                    .PutJsonAsync(request)
                                    .CatchUserActionError()
                                    .ReceiveJson<UserDetailResponse>();

        //Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdUser.Id);

        // whitelisted-поля обновляются
        result.FirstName.Should().Be(request.FirstName);
        result.LastName.Should().Be(request.LastName);
        result.MiddleName.Should().Be(request.MiddleName);
        result.PhoneNumber.Should().Be(request.PhoneNumber);
        result.BirthDate.Should().Be(request.BirthDate);
        result.Gender.Should().Be(request.Gender);
        result.AvatarUrl.Should().Be(request.AvatarUrl);
        result.About.Should().Be(request.About);

        // защищённые поля остаются от текущего пользователя
        result.UserName.Should().Be(userNameBefore);
        result.Email.Should().Be(emailBefore);
        result.Type.Should().Be(typeBefore);
        result.Roles.Should().BeEquivalentTo([role.Name]);
    }

    [IntegrationFact]
    public async Task UpdateProfile_LegacyEmailUserName_Succeeds()
    {
        //Arrange — легаси-пользователь с username = почта (старый сид мастера):
        //валидатор не должен блокировать сохранение профиля, пока имя не меняют
        _ = nameof(AccountController.UpdateProfile);
        var createdUser = CreateUserWithRole(out var role, userName: "legacy@mail.localhost");
        var client = AppFixture.GetClientForUser(createdUser.Id);

        var request = CreateValidRequest();

        //Act
        var result = await client.Request(_apiUrl, "Profile")
                                    .PutJsonAsync(request)
                                    .CatchUserActionError()
                                    .ReceiveJson<UserDetailResponse>();

        //Assert
        result.FirstName.Should().Be(request.FirstName);
        result.LastName.Should().Be(request.LastName);
        result.UserName.Should().Be("legacy@mail.localhost");
        result.Roles.Should().BeEquivalentTo([role.Name]);
    }

    [IntegrationFact]
    public async Task UpdateProfile_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(AccountController.UpdateProfile);
        var client = AppFixture.GetClient();

        var request = CreateValidRequest() with { FirstName = string.Empty };

        //Act
        var result = await client.Request(_apiUrl, "Profile").PutJsonAsync(request).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(UpdateUserProfileRequest.FirstName)] = ["*обязательно*"],
        });
    }

    UserEntity CreateUserWithRole(out RoleEntity role, string? userName = null)
    {
        var ef = AppFixture.MarsDbContext();

        var userType = ef.UserTypes.First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        role = ef.Roles.AsNoTracking().First();
        var createdUser = _fixture.Create<UserEntity>();
        createdUser.MetaValues = [];
        createdUser.UserTypeId = userType.Id;
        if (userName is not null)
        {
            createdUser.UserName = userName;
            createdUser.NormalizedUserName = userName.ToUpper();
        }
        ef.Users.Add(createdUser);
        ef.UserRoles.Add(new UserRoleEntity { UserId = createdUser.Id, RoleId = role.Id });
        ef.SaveChanges();
        return createdUser;
    }

    static UpdateUserProfileRequest CreateValidRequest()
        => new()
        {
            FirstName = "ProfileFirst",
            LastName = "ProfileLast",
            MiddleName = "ProfileMiddle",
            PhoneNumber = "+79991112233",
            BirthDate = new DateTime(1990, 5, 15),
            Gender = UserGender.Female,
            AvatarUrl = null,
            About = "Расскажу о себе",
            MetaValues = [],
        };
}
