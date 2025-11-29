using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Repositories;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.SSO.Controllers;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Modules.SSO;

public class SSOUpdateUserEntityTests : ApplicationTests
{
    private readonly IUserRepository _userRepository;

    public SSOUpdateUserEntityTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _userRepository = appFixture.ServiceProvider.GetRequiredService<IUserRepository>();
    }

    [IntegrationFact]
    public async Task RemoteUserUpsert_CreateUserRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SsoController.Callback);
        _ = nameof(UserRepository.RemoteUserUpsert);
        var ef = AppFixture.MarsDbContext();
        var newRole = _fixture.Create<RoleEntity>();
        ef.Roles.Add(newRole);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var query = _fixture.Build<UpsertUserRemoteDataQuery>()
                            .With(s => s.Roles, [newRole.Name!])
                            .With(s => s.PhoneNumber, "+9999")
                            .Create();

        //Act
        var userInfo = await _userRepository.RemoteUserUpsert(query, default);

        //Assert
        await AssertCreatedUser(query, userInfo);

    }

    [IntegrationFact]
    public async Task RemoteUserUpsert_UpdateExistUserRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SsoController.Callback);
        _ = nameof(UserRepository.RemoteUserUpsert);
        var ef = AppFixture.MarsDbContext();
        string userExternalKey = "external-" + Guid.NewGuid().ToString("N");
        var provider = _fixture.Create<SsoProviderInfo>();

        var createdUser = _fixture.Create<UserEntity>();
        createdUser.Logins = [new UserLoginEntity {
            User = createdUser,
            LoginProvider = provider.ProviderSlug,
            ProviderKey = userExternalKey,
            ProviderDisplayName = provider.DisplayName
        }];
        ef.Users.Add(createdUser);
        var newRoles = _fixture.CreateMany<RoleEntity>().ToList();
        ef.Roles.AddRange(newRoles);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var query = _fixture.Build<UpsertUserRemoteDataQuery>()
                            .With(s => s.Roles, newRoles.Select(s => s.Name).ToList()!)
                            .With(s => s.ExternalKey, userExternalKey)
                            .With(s => s.Prodvider, provider)
                            .With(s => s.PhoneNumber, "+99992")
                            .Create();

        //Act
        var userInfo = await _userRepository.RemoteUserUpsert(query, default);

        //Assert
        createdUser.Id.Should().Be(userInfo.Id);
        await AssertCreatedUser(query, userInfo);

    }

    private async Task AssertCreatedUser(UpsertUserRemoteDataQuery query, AuthorizedUserInformationDto userInfo)
    {
        var ef = AppFixture.MarsDbContext();
        var user = await _userRepository.GetDetail(userInfo.Id, default);
        var userLogin = await ef.UserLogins.FirstOrDefaultAsync(s => s.UserId == userInfo.Id);

        userLogin.ProviderKey.Should().Be(query.ExternalKey);
        userLogin.LoginProvider.Should().Be(query.Prodvider.ProviderSlug);
        userLogin.ProviderDisplayName.Should().Be(query.Prodvider.DisplayName);

        user.FirstName.Should().Be(query.FirstName);
        user.LastName.Should().Be(query.LastName);
        user.MiddleName.Should().Be(query.MiddleName);
        user.Email.Should().Be(query.Email);
        user.Roles.Should().BeEquivalentTo(query.Roles);
        user.BirthDate.Should().BeCloseTo(query.BirthDate!.Value, TimeSpan.FromMilliseconds(50));
        user.Gender.Should().Be(query.Gender);
        user.PhoneNumber.Should().Be(query.PhoneNumber);
        user.AvatarUrl.Should().Be(query.AvatarUrl);

        // alse check is returned value equal in db
        userInfo.Should().BeEquivalentTo(user, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UserDetail>()
            .ExcludingMissingMembers());
    }
}
