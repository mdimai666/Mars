using FluentAssertions;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.Options;
using Mars.Identity.Host.Services;
using Mars.Options.Abstractions.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Mars.Integration.Tests.Services;

public class PasskeyServiceTests
{
    private readonly IUserStore<UserEntity> _store;
    private readonly IUserPasskeyStore<UserEntity> _passkeyStore;
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IOptionService _optionService = Substitute.For<IOptionService>();
    private readonly PasskeyService _service;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly UserEntity _user;

    public PasskeyServiceTests()
    {
        _user = new UserEntity
        {
            Id = _userId,
            UserName = "test@test",
            Email = "test@test",
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
        };

        _store = Substitute.For<IUserStore<UserEntity>, IUserPasskeyStore<UserEntity>>();
        _passkeyStore = (IUserPasskeyStore<UserEntity>)_store;

        _store.FindByIdAsync(_userId.ToString(), Arg.Any<CancellationToken>()).Returns(_user);
        _store.UpdateAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>()).Returns(IdentityResult.Success);

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

        _signInManager.SignInAsync(Arg.Any<UserEntity>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(Task.CompletedTask);

        _optionService.GetOption<PasskeyOption>().Returns(new PasskeyOption { PasskeysMaxPerUser = 2 });

        _service = new PasskeyService(_userManager, _signInManager, _tokenService, _userRepository, _optionService);
    }

    private static UserPasskeyInfo TestPasskeyInfo(byte[]? credentialId = null) => new(
        credentialId ?? [1, 2, 3],
        [4, 5, 6],
        DateTimeOffset.UtcNow,
        0,
        [],
        true,
        true,
        false,
        [],
        []);

    [Fact]
    public async Task CompleteRegister_RejectsWhenAttestationFails()
    {
        _signInManager.PerformPasskeyAttestationAsync("bad-json")
            .Returns(PasskeyAttestationResult.Fail(new PasskeyException("attestation failed")));

        var result = await _service.CompleteRegister(_userId, "bad-json", null);

        result.Ok.Should().BeFalse();
        result.Message.Should().Be("attestation failed");
        await _passkeyStore.DidNotReceive().AddOrUpdatePasskeyAsync(Arg.Any<UserEntity>(), Arg.Any<UserPasskeyInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteRegister_RejectsWhenOwnershipMismatch()
    {
        _signInManager.PerformPasskeyAttestationAsync(Arg.Any<string>())
            .Returns(PasskeyAttestationResult.Success(
                TestPasskeyInfo(),
                new PasskeyUserEntity { Id = Guid.NewGuid().ToString(), Name = "other", DisplayName = "other" }));

        var result = await _service.CompleteRegister(_userId, "credential", null);

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("не на текущего пользователя");
        await _passkeyStore.DidNotReceive().AddOrUpdatePasskeyAsync(Arg.Any<UserEntity>(), Arg.Any<UserPasskeyInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteRegister_RejectsWhenLimitReached()
    {
        _signInManager.PerformPasskeyAttestationAsync(Arg.Any<string>())
            .Returns(PasskeyAttestationResult.Success(
                TestPasskeyInfo(),
                new PasskeyUserEntity { Id = _userId.ToString(), Name = "test", DisplayName = "test" }));
        _passkeyStore.GetPasskeysAsync(_user, Arg.Any<CancellationToken>())
            .Returns(new List<UserPasskeyInfo> { TestPasskeyInfo([9]), TestPasskeyInfo([8]) });

        var result = await _service.CompleteRegister(_userId, "credential", null);

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("лимит");
        await _passkeyStore.DidNotReceive().AddOrUpdatePasskeyAsync(Arg.Any<UserEntity>(), Arg.Any<UserPasskeyInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteRegister_StoresPasskeyWithNormalizedName()
    {
        var passkey = TestPasskeyInfo();
        _signInManager.PerformPasskeyAttestationAsync("credential")
            .Returns(PasskeyAttestationResult.Success(passkey, new PasskeyUserEntity { Id = _userId.ToString(), Name = "test", DisplayName = "test" }));
        _passkeyStore.GetPasskeysAsync(_user, Arg.Any<CancellationToken>())
            .Returns(new List<UserPasskeyInfo> { TestPasskeyInfo([9]) });

        var result = await _service.CompleteRegister(_userId, "credential", "  Мой iPhone  ");

        result.Ok.Should().BeTrue();
        await _passkeyStore.Received(1).AddOrUpdatePasskeyAsync(_user, passkey, Arg.Any<CancellationToken>());
        passkey.Name.Should().Be("Мой iPhone");
    }

    [Fact]
    public async Task CompleteRegister_RejectsWhenUserNotFound()
    {
        var result = await _service.CompleteRegister(Guid.NewGuid(), "credential", null);

        result.Ok.Should().BeFalse();
        result.Message.Should().Be("Пользователь не найден");
    }

    [Fact]
    public async Task Rename_RejectsInvalidCredentialId()
    {
        var result = await _service.Rename(_userId, "!!!not-base64url!!!", "new-name");

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("Неверный идентификатор");
    }

    [Fact]
    public async Task Rename_UpdatesName()
    {
        var credentialId = new byte[] { 1, 2, 3 };
        var passkey = TestPasskeyInfo(credentialId);
        _passkeyStore.FindPasskeyAsync(_user, Arg.Is<byte[]>(b => b.SequenceEqual(credentialId)), Arg.Any<CancellationToken>())
            .Returns(passkey);

        var result = await _service.Rename(_userId, Base64UrlEncoder.Encode(credentialId), "Work key");

        result.Ok.Should().BeTrue();
        passkey.Name.Should().Be("Work key");
        await _passkeyStore.Received(1).AddOrUpdatePasskeyAsync(_user, passkey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_RemovesPasskey()
    {
        var credentialId = new byte[] { 7, 7, 7 };

        var result = await _service.Delete(_userId, Base64UrlEncoder.Encode(credentialId));

        result.Ok.Should().BeTrue();
        await _passkeyStore.Received(1).RemovePasskeyAsync(_user, Arg.Is<byte[]>(b => b.SequenceEqual(credentialId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_FailsOnAssertionFailure()
    {
        _signInManager.PerformPasskeyAssertionAsync("bad")
            .Returns(PasskeyAssertionResult.Fail<UserEntity>(new PasskeyException("assertion failed")));

        var result = await _service.Login("bad", CancellationToken.None);

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("assertion failed");
        await _signInManager.DidNotReceive().SignInAsync(Arg.Any<UserEntity>(), Arg.Any<bool>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Login_ReturnsToken_AndPersistsUpdatedPasskey()
    {
        var passkey = TestPasskeyInfo();
        _signInManager.PerformPasskeyAssertionAsync("credential")
            .Returns(PasskeyAssertionResult.Success(passkey, _user));
        _signInManager.CanSignInAsync(_user).Returns(true);
        _tokenService.CreateAccessToken(_userId, _userRepository, Arg.Any<CancellationToken>()).Returns("test-token");
        _tokenService.JwtExpireUnixSeconds().Returns(3600);

        var result = await _service.Login("credential", CancellationToken.None);

        result.IsAuthSuccessful.Should().BeTrue();
        result.Token.Should().Be("test-token");
        result.ExpiresIn.Should().Be(3600);
        await _passkeyStore.Received(1).AddOrUpdatePasskeyAsync(_user, passkey, Arg.Any<CancellationToken>());
        await _signInManager.Received(1).SignInAsync(_user, true, Arg.Any<string?>());
    }

    [Fact]
    public async Task Login_RejectsWhenSignInNotAllowed()
    {
        _signInManager.PerformPasskeyAssertionAsync("credential")
            .Returns(PasskeyAssertionResult.Success(TestPasskeyInfo(), _user));
        _signInManager.CanSignInAsync(_user).Returns(false);

        var result = await _service.Login("credential", CancellationToken.None);

        result.IsAuthSuccessful.Should().BeFalse();
        await _signInManager.DidNotReceive().SignInAsync(Arg.Any<UserEntity>(), Arg.Any<bool>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Login_RejectsWhenPasskeysDisabled()
    {
        _optionService.GetOption<PasskeyOption>().Returns(new PasskeyOption { Enabled = false });

        var result = await _service.Login("credential", CancellationToken.None);

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("Пасскеи отключены");
        await _signInManager.DidNotReceive().PerformPasskeyAssertionAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task CompleteRegister_RejectsWhenPasskeysDisabled()
    {
        _optionService.GetOption<PasskeyOption>().Returns(new PasskeyOption { Enabled = false });

        var result = await _service.CompleteRegister(_userId, "credential", null);

        result.Ok.Should().BeFalse();
        result.Message.Should().Be("Пасскеи отключены");
        await _passkeyStore.DidNotReceive().AddOrUpdatePasskeyAsync(Arg.Any<UserEntity>(), Arg.Any<UserPasskeyInfo>(), Arg.Any<CancellationToken>());
    }
}
