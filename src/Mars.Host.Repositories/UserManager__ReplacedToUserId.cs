using System.Security.Claims;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mars.Host.Repositories;

internal class UserManager__ReplacedToUserId : UserManager<UserEntity>, IUserManager
{
    public UserManager__ReplacedToUserId(IUserStore<UserEntity> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<UserEntity> passwordHasher, IEnumerable<IUserValidator<UserEntity>> userValidators, IEnumerable<IPasswordValidator<UserEntity>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<UserEntity>> logger) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
    }

    UserEntity GetUser(Guid userId) => Store.FindByIdAsync(userId.ToString(), CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult()!;

    public Task<IdentityResult> AccessFailedAsync(Guid userId) => AccessFailedAsync(GetUser(userId));
    public Task<IdentityResult> AddClaimAsync(Guid userId, Claim claim) => AddClaimAsync(GetUser(userId), claim);
    public Task<IdentityResult> AddClaimsAsync(Guid userId, IEnumerable<Claim> claims) => AddClaimsAsync(GetUser(userId), claims);
    public Task<IdentityResult> AddLoginAsync(Guid userId, UserLoginInfo login) => AddLoginAsync(GetUser(userId), login);
    public Task<IdentityResult> AddPasswordAsync(Guid userId, string password) => AddPasswordAsync(GetUser(userId), password);
    public Task<IdentityResult> AddToRoleAsync(Guid userId, string role) => AddToRoleAsync(GetUser(userId), role);
    public Task<IdentityResult> AddToRolesAsync(Guid userId, IEnumerable<string> roles) => AddToRolesAsync(GetUser(userId), roles);
    public Task<IdentityResult> ChangeEmailAsync(Guid userId, string newEmail, string token) => ChangeEmailAsync(GetUser(userId), newEmail, token);
    public Task<IdentityResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword) => ChangePasswordAsync(GetUser(userId), currentPassword, newPassword);
    public Task<IdentityResult> ChangePhoneNumberAsync(Guid userId, string phoneNumber, string token) => ChangePhoneNumberAsync(GetUser(userId), phoneNumber, token);
    public Task<bool> CheckPasswordAsync(Guid userId, string password) => CheckPasswordAsync(GetUser(userId), password);
    public Task<IdentityResult> ConfirmEmailAsync(Guid userId, string token) => ConfirmEmailAsync(GetUser(userId), token);
    public Task<int> CountRecoveryCodesAsync(Guid userId) => CountRecoveryCodesAsync(GetUser(userId));
    public Task<IdentityResult> CreateAsync(Guid userId) => CreateAsync(GetUser(userId));
    public Task<IdentityResult> CreateAsync(Guid userId, string password) => CreateAsync(GetUser(userId));
    public Task<byte[]> CreateSecurityTokenAsync(Guid userId) => CreateSecurityTokenAsync(GetUser(userId));
    public Task<IdentityResult> DeleteAsync(Guid userId) => DeleteAsync(GetUser(userId));
    public Task<string> GenerateChangeEmailTokenAsync(Guid userId, string newEmail) => GenerateChangeEmailTokenAsync(GetUser(userId), newEmail);
    public Task<string> GenerateChangePhoneNumberTokenAsync(Guid userId, string phoneNumber) => GenerateChangePhoneNumberTokenAsync(GetUser(userId), phoneNumber);
    public Task<string> GenerateConcurrencyStampAsync(Guid userId) => GenerateConcurrencyStampAsync(GetUser(userId));
    public Task<string> GenerateEmailConfirmationTokenAsync(Guid userId) => GenerateEmailConfirmationTokenAsync(GetUser(userId));
    public Task<IEnumerable<string>?> GenerateNewTwoFactorRecoveryCodesAsync(Guid userId, int number) => GenerateNewTwoFactorRecoveryCodesAsync(GetUser(userId), number);
    public Task<string> GeneratePasswordResetTokenAsync(Guid userId) => GeneratePasswordResetTokenAsync(GetUser(userId));
    public Task<string> GenerateTwoFactorTokenAsync(Guid userId, string tokenProvider) => GenerateTwoFactorTokenAsync(GetUser(userId), tokenProvider);
    public Task<string> GenerateUserTokenAsync(Guid userId, string tokenProvider, string purpose) => GenerateUserTokenAsync(GetUser(userId), tokenProvider, purpose);
    public Task<int> GetAccessFailedCountAsync(Guid userId) => GetAccessFailedCountAsync(GetUser(userId));
    public Task<string?> GetAuthenticationTokenAsync(Guid userId, string loginProvider, string tokenName) => GetAuthenticationTokenAsync(GetUser(userId), loginProvider, tokenName);
    public Task<string?> GetAuthenticatorKeyAsync(Guid userId) => GetAuthenticatorKeyAsync(GetUser(userId));
    public Task<IList<Claim>> GetClaimsAsync(Guid userId) => GetClaimsAsync(GetUser(userId));
    public Task<string?> GetEmailAsync(Guid userId) => GetEmailAsync(GetUser(userId));
    public Task<bool> GetLockoutEnabledAsync(Guid userId) => GetLockoutEnabledAsync(GetUser(userId));
    public Task<DateTimeOffset?> GetLockoutEndDateAsync(Guid userId) => GetLockoutEndDateAsync(GetUser(userId));
    public Task<IList<UserLoginInfo>> GetLoginsAsync(Guid userId) => GetLoginsAsync(GetUser(userId));
    public Task<string?> GetPhoneNumberAsync(Guid userId) => GetPhoneNumberAsync(GetUser(userId));
    public Task<IList<string>> GetRolesAsync(Guid userId) => GetRolesAsync(GetUser(userId));
    public Task<string> GetSecurityStampAsync(Guid userId) => GetSecurityStampAsync(GetUser(userId));
    public Task<bool> GetTwoFactorEnabledAsync(Guid userId) => GetTwoFactorEnabledAsync(GetUser(userId));
    public Task<string> GetUserIdAsync(Guid userId) => GetUserIdAsync(GetUser(userId));
    public Task<string?> GetUserNameAsync(Guid userId) => GetUserNameAsync(GetUser(userId));
    public Task<IList<string>> GetValidTwoFactorProvidersAsync(Guid userId) => GetValidTwoFactorProvidersAsync(GetUser(userId));
    public Task<bool> HasPasswordAsync(Guid userId) => HasPasswordAsync(GetUser(userId));
    public Task<bool> IsEmailConfirmedAsync(Guid userId) => IsEmailConfirmedAsync(GetUser(userId));
    public Task<bool> IsInRoleAsync(Guid userId, string role) => IsInRoleAsync(GetUser(userId), role);
    public Task<bool> IsLockedOutAsync(Guid userId) => IsLockedOutAsync(GetUser(userId));
    public Task<bool> IsPhoneNumberConfirmedAsync(Guid userId) => IsPhoneNumberConfirmedAsync(GetUser(userId));
    public Task<IdentityResult> RedeemTwoFactorRecoveryCodeAsync(Guid userId, string code) => RedeemTwoFactorRecoveryCodeAsync(GetUser(userId), code);
    public Task<IdentityResult> RemoveAuthenticationTokenAsync(Guid userId, string loginProvider, string tokenName) => RemoveAuthenticationTokenAsync(GetUser(userId), loginProvider, tokenName);
    public Task<IdentityResult> RemoveClaimAsync(Guid userId, Claim claim) => RemoveClaimAsync(GetUser(userId), claim);
    public Task<IdentityResult> RemoveClaimsAsync(Guid userId, IEnumerable<Claim> claims) => RemoveClaimsAsync(GetUser(userId), claims);
    public Task<IdentityResult> RemoveFromRoleAsync(Guid userId, string role) => RemoveFromRoleAsync(GetUser(userId), role);
    public Task<IdentityResult> RemoveFromRolesAsync(Guid userId, IEnumerable<string> roles) => RemoveFromRolesAsync(GetUser(userId), roles);
    public Task<IdentityResult> RemoveLoginAsync(Guid userId, string loginProvider, string providerKey) => RemoveLoginAsync(GetUser(userId), loginProvider, providerKey);
    public Task<IdentityResult> RemovePasswordAsync(Guid userId) => RemovePasswordAsync(GetUser(userId));
    public Task<IdentityResult> ReplaceClaimAsync(Guid userId, Claim claim, Claim newClaim) => ReplaceClaimAsync(GetUser(userId), claim, newClaim);
    public Task<IdentityResult> ResetAccessFailedCountAsync(Guid userId) => ResetAccessFailedCountAsync(GetUser(userId));
    public Task<IdentityResult> ResetAuthenticatorKeyAsync(Guid userId) => ResetAuthenticatorKeyAsync(GetUser(userId));
    public Task<IdentityResult> ResetPasswordAsync(Guid userId, string token, string newPassword) => ResetPasswordAsync(GetUser(userId), token, newPassword);
    public Task<IdentityResult> SetAuthenticationTokenAsync(Guid userId, string loginProvider, string tokenName, string? tokenValue) => SetAuthenticationTokenAsync(GetUser(userId), loginProvider, tokenName, tokenValue);
    public Task<IdentityResult> SetEmailAsync(Guid userId, string? email) => SetEmailAsync(GetUser(userId), email);
    public Task<IdentityResult> SetLockoutEnabledAsync(Guid userId, bool enabled) => SetLockoutEnabledAsync(GetUser(userId), enabled);
    public Task<IdentityResult> SetLockoutEndDateAsync(Guid userId, DateTimeOffset? lockoutEnd) => SetLockoutEndDateAsync(GetUser(userId), lockoutEnd);
    public Task<IdentityResult> SetPhoneNumberAsync(Guid userId, string? phoneNumber) => SetPhoneNumberAsync(GetUser(userId), phoneNumber);
    public Task<IdentityResult> SetTwoFactorEnabledAsync(Guid userId, bool enabled) => SetTwoFactorEnabledAsync(GetUser(userId), enabled);
    public Task<IdentityResult> SetUserNameAsync(Guid userId, string? userName) => SetUserNameAsync(GetUser(userId), userName);
    public Task<IdentityResult> UpdateAsync(Guid userId) => UpdateAsync(GetUser(userId));
    public Task UpdateNormalizedEmailAsync(Guid userId) => UpdateNormalizedEmailAsync(GetUser(userId));
    public Task UpdateNormalizedUserNameAsync(Guid userId) => UpdateNormalizedUserNameAsync(GetUser(userId));
    public Task<IdentityResult> UpdateSecurityStampAsync(Guid userId) => UpdateSecurityStampAsync(GetUser(userId));
    public Task<bool> VerifyChangePhoneNumberTokenAsync(Guid userId, string token, string phoneNumber) => VerifyChangePhoneNumberTokenAsync(GetUser(userId), token, phoneNumber);
    public Task<bool> VerifyTwoFactorTokenAsync(Guid userId, string tokenProvider, string token) => VerifyTwoFactorTokenAsync(GetUser(userId), tokenProvider, token);
    public Task<bool> VerifyUserTokenAsync(Guid userId, string tokenProvider, string purpose, string token) => VerifyUserTokenAsync(GetUser(userId), tokenProvider, purpose, token);

    // new remapped api
    async Task<UserDetail?> IUserManager.FindByEmailAsync(string email)
        => (await FindByEmailAsync(email))?.ToDetail();
    async Task<UserDetail?> IUserManager.FindByIdAsync(string userId)
        => (await FindByIdAsync(userId))?.ToDetail();
    async Task<UserDetail?> IUserManager.FindByLoginAsync(string loginProvider, string providerKey)
        => (await FindByLoginAsync(loginProvider, providerKey))?.ToDetail();
    async Task<UserDetail?> IUserManager.FindByNameAsync(string userName)
        => (await FindByNameAsync(userName))?.ToDetail();
    async Task<UserDetail?> IUserManager.GetUserAsync(ClaimsPrincipal principal)
        => (await GetUserAsync(principal))?.ToDetail();
    async Task<IList<UserDetail>> IUserManager.GetUsersForClaimAsync(Claim claim)
        => (await GetUsersForClaimAsync(claim)).Select(UserMapping.ToDetail).ToList();
    async Task<IList<UserDetail>> IUserManager.GetUsersInRoleAsync(string roleName)
        => (await GetUsersInRoleAsync(roleName)).Select(UserMapping.ToDetail).ToList();
}
