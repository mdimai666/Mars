using FluentAssertions;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.Auth;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Host.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Mars.Integration.Tests.Services;

public class AccountsServiceTests
{
    private readonly IUserStore<UserEntity> _store;
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AccountsService _service;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly UserEntity _user;

    public AccountsServiceTests()
    {
        _user = new UserEntity
        {
            Id = _userId,
            UserName = "test@test",
            Email = "test@test",
            EmailConfirmed = true,
            LockoutEnabled = true,
            FirstName = "Test",
            LastName = "User",
        };

        _store = Substitute.For<IUserStore<UserEntity>, IUserEmailStore<UserEntity>>();
        _store.FindByNameAsync("TEST@TEST", Arg.Any<CancellationToken>()).Returns(_user);

        _userManager = Substitute.ForPartsOf<UserManager<UserEntity>>(
            _store,
            OptionsFactory.Create(new IdentityOptions()),
            Substitute.For<IPasswordHasher<UserEntity>>(),
            Array.Empty<IUserValidator<UserEntity>>(),
            Array.Empty<IPasswordValidator<UserEntity>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            NullLogger<UserManager<UserEntity>>.Instance);

        _signInManager = Substitute.ForPartsOf<SignInManager<UserEntity>>(
            _userManager,
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<UserEntity>>(),
            OptionsFactory.Create(new IdentityOptions()),
            NullLogger<SignInManager<UserEntity>>.Instance,
            Substitute.For<IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<UserEntity>>());

        // без DoNotCallBase стаббинг исполнил бы реальную реализацию SignInManager
        _signInManager
            .When(x => x.PasswordSignInAsync(Arg.Any<UserEntity>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>()))
            .DoNotCallBase();
        _signInManager
            .When(x => x.CheckPasswordSignInAsync(Arg.Any<UserEntity>(), Arg.Any<string>(), Arg.Any<bool>()))
            .DoNotCallBase();

        _tokenService.CreateAccessToken(Arg.Any<Guid>(), Arg.Any<IUserRepository>(), Arg.Any<CancellationToken>())
            .Returns("jwt-token");
        _tokenService.JwtExpireUnixSeconds().Returns(123);

        _service = new AccountsService(
            _userManager,
            _signInManager,
            Substitute.For<IHttpContextAccessor>(),
            _tokenService,
            Substitute.For<IUserRepository>(),
            Substitute.For<IUserTypeRepository>());
    }

    private static AuthCredentialsDto Credentials(string login = "test@test", string password = "pwd")
        => new() { Login = login, Password = password };

    [Fact]
    public async Task Login_UnknownUser_ReturnsInvalidData_WithoutSignIn()
    {
        var result = await _service.Login(Credentials(login: "nobody"), CancellationToken.None);

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("Неверные данные");
        await _signInManager.DidNotReceive()
            .PasswordSignInAsync(Arg.Any<UserEntity>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsInvalidData_WithLockoutOnFailure()
    {
        _signInManager.PasswordSignInAsync(_user, "pwd", true, true).Returns(SignInResult.Failed);

        var result = await _service.Login(Credentials(), CancellationToken.None);

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("Неверные данные");
        await _signInManager.Received(1).PasswordSignInAsync(_user, "pwd", isPersistent: true, lockoutOnFailure: true);
    }

    [Fact]
    public async Task Login_LockedOut_ReturnsLockoutMessage()
    {
        _signInManager.PasswordSignInAsync(_user, "pwd", true, true).Returns(SignInResult.LockedOut);

        var result = await _service.Login(Credentials(), CancellationToken.None);

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("заблокирована");
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task Login_Success_ReturnsToken()
    {
        _signInManager.PasswordSignInAsync(_user, "pwd", true, true).Returns(SignInResult.Success);

        var result = await _service.Login(Credentials(), CancellationToken.None);

        result.IsAuthSuccessful.Should().BeTrue();
        result.Token.Should().Be("jwt-token");
        result.ExpiresIn.Should().Be(123);
        result.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task ValidateUserCredentials_Success_ReturnsId_WithLockoutOnFailure()
    {
        _signInManager.CheckPasswordSignInAsync(_user, "pwd", true).Returns(SignInResult.Success);

        var userId = await _service.ValidateUserCredentials("test@test", "pwd", CancellationToken.None);

        userId.Should().Be(_userId);
        await _signInManager.Received(1).CheckPasswordSignInAsync(_user, "pwd", lockoutOnFailure: true);
    }

    [Fact]
    public async Task ValidateUserCredentials_Failed_ReturnsNull()
    {
        _signInManager.CheckPasswordSignInAsync(_user, "pwd", true).Returns(SignInResult.Failed);

        var userId = await _service.ValidateUserCredentials("test@test", "pwd", CancellationToken.None);

        userId.Should().BeNull();
    }

    [Fact]
    public async Task ValidateUserCredentials_LockedOut_ReturnsNull()
    {
        _signInManager.CheckPasswordSignInAsync(_user, "pwd", true).Returns(SignInResult.LockedOut);

        var userId = await _service.ValidateUserCredentials("test@test", "pwd", CancellationToken.None);

        userId.Should().BeNull();
    }
}
