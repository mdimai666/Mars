using Mars.Contracts.Common;
using Mars.Core.Exceptions;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.Auth;
using Mars.Identity.Abstractions.Dto.Passkeys;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.Options;
using Mars.Options.Abstractions.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Mars.Identity.Host.Services;

internal class PasskeyService : IPasskeyService
{
    private const int NameMaxLength = 64;

    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly IOptionService _optionService;

    public PasskeyService(
        UserManager<UserEntity> userManager,
        SignInManager<UserEntity> signInManager,
        ITokenService tokenService,
        IUserRepository userRepository,
        IOptionService optionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _userRepository = userRepository;
        _optionService = optionService;
    }

    public async Task<IReadOnlyCollection<PasskeySummary>> List(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return [];

        var passkeys = await _userManager.GetPasskeysAsync(user);
        return passkeys.Select(ToSummary).ToList();
    }

    public async Task<string?> BeginRegister(Guid userId)
    {
        var option = _optionService.GetOption<PasskeyOption>();
        if (!option.Enabled)
            throw new UserActionException("Пасскеи отключены");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return null;

        var existing = await _userManager.GetPasskeysAsync(user);
        if (existing.Count >= option.PasskeysMaxPerUser)
            throw new UserActionException($"Достигнут лимит пасскеев на пользователя ({option.PasskeysMaxPerUser})");

        return await _signInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = user.Id.ToString(),
            Name = user.UserName ?? user.Email ?? "user",
            DisplayName = BuildDisplayName(user),
        });
    }

    public async Task<UserActionResult> CompleteRegister(Guid userId, string credentialJson, string? name)
    {
        var option = _optionService.GetOption<PasskeyOption>();
        if (!option.Enabled)
            return UserActionResult.Exception("Пасскеи отключены", null);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return UserActionResult.Exception("Пользователь не найден", null);

        var attestation = await _signInManager.PerformPasskeyAttestationAsync(credentialJson);
        if (!attestation.Succeeded)
            return UserActionResult.Exception(attestation.Failure?.Message ?? "Ошибка регистрации пасскея", null);

        if (!string.Equals(attestation.UserEntity?.Id, user.Id.ToString(), StringComparison.Ordinal))
            return UserActionResult.Exception("Пасскей зарегистрирован не на текущего пользователя", null);

        var limit = option.PasskeysMaxPerUser;
        var existing = await _userManager.GetPasskeysAsync(user);
        if (existing.Count >= limit)
            return UserActionResult.Exception($"Достигнут лимит пасскеев на пользователя ({limit})", null);

        var passkey = attestation.Passkey;
        passkey.Name = NormalizeName(name);

        var result = await _userManager.AddOrUpdatePasskeyAsync(user, passkey);
        return result.Succeeded
            ? UserActionResult.Success("Пасскей добавлен")
            : UserActionResult.Exception("Не удалось сохранить пасскей", null);
    }

    public async Task<UserActionResult> Rename(Guid userId, string credentialId, string name)
    {
        if (!TryDecodeCredentialId(credentialId, out var credentialIdBytes))
            return UserActionResult.Exception("Неверный идентификатор пасскея", null);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return UserActionResult.Exception("Пользователь не найден", null);

        var passkey = await _userManager.GetPasskeyAsync(user, credentialIdBytes);
        if (passkey is null)
            return UserActionResult.Exception("Пасскей не найден", null);

        passkey.Name = NormalizeName(name);

        var result = await _userManager.AddOrUpdatePasskeyAsync(user, passkey);
        return result.Succeeded
            ? UserActionResult.Success("Переименовано")
            : UserActionResult.Exception("Не удалось сохранить пасскей", null);
    }

    public async Task<UserActionResult> Delete(Guid userId, string credentialId)
    {
        if (!TryDecodeCredentialId(credentialId, out var credentialIdBytes))
            return UserActionResult.Exception("Неверный идентификатор пасскея", null);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return UserActionResult.Exception("Пользователь не найден", null);

        var result = await _userManager.RemovePasskeyAsync(user, credentialIdBytes);
        return result.Succeeded
            ? UserActionResult.SuccessDeleted()
            : UserActionResult.Exception("Пасскей не найден", null);
    }

    public Task<string> BeginLogin()
    {
        if (!_optionService.GetOption<PasskeyOption>().Enabled)
            throw new UserActionException("Пасскеи отключены");

        return _signInManager.MakePasskeyRequestOptionsAsync(user: null);
    }

    public async Task<AuthResultDto> Login(string credentialJson, CancellationToken cancellationToken)
    {
        if (!_optionService.GetOption<PasskeyOption>().Enabled)
            return AuthResultDto.ErrorResponse("Пасскеи отключены");

        var assertion = await _signInManager.PerformPasskeyAssertionAsync(credentialJson);
        if (!assertion.Succeeded || assertion.User is null)
            return AuthResultDto.ErrorResponse(assertion.Failure?.Message ?? "Ошибка входа по пасскею");

        var user = assertion.User;

        if (!await _signInManager.CanSignInAsync(user))
            return AuthResultDto.ErrorResponse("Вход для пользователя запрещён");

        if (assertion.Passkey is not null)
            await _userManager.AddOrUpdatePasskeyAsync(user, assertion.Passkey);

        var token = await _tokenService.CreateAccessToken(user.Id, _userRepository, cancellationToken);
        await _signInManager.SignInAsync(user, true);

        return new AuthResultDto
        {
            Token = token,
            ExpiresIn = _tokenService.JwtExpireUnixSeconds(),
            ErrorMessage = null,
            RefreshToken = null,
        };
    }

    private static PasskeySummary ToSummary(UserPasskeyInfo passkey) => new()
    {
        CredentialId = Base64UrlEncoder.Encode(passkey.CredentialId),
        Name = passkey.Name,
        CreatedAt = passkey.CreatedAt,
        IsBackedUp = passkey.IsBackedUp,
        IsBackupEligible = passkey.IsBackupEligible,
    };

    private static bool TryDecodeCredentialId(string credentialId, out byte[] credentialIdBytes)
    {
        try
        {
            credentialIdBytes = Base64UrlEncoder.DecodeBytes(credentialId);
            return credentialIdBytes.Length > 0;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            credentialIdBytes = [];
            return false;
        }
    }

    private static string? NormalizeName(string? name)
    {
        name = name?.Trim();
        if (string.IsNullOrEmpty(name))
            return null;
        return name.Length <= NameMaxLength ? name : name[..NameMaxLength];
    }

    private static string BuildDisplayName(UserEntity user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return fullName.Length > 0 ? fullName : user.UserName ?? user.Email ?? "user";
    }
}
