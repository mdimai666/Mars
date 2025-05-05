using System.Security.Claims;
using Mars.Host.Shared.Dto.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Shared.Services;

public interface IUserManager
{
    public const string ResetPasswordTokenPurpose = "ResetPassword";
    public const string ChangePhoneNumberTokenPurpose = "ChangePhoneNumber";
    public const string ConfirmEmailTokenPurpose = "EmailConfirmation";

    //public UserManager(IUserStore<UserEntity> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<UserEntity> passwordHasher, IEnumerable<IUserValidator<UserEntity>> userValidators, IEnumerable<IPasswordValidator<UserEntity>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<UserEntity>> logger);

    //public IPasswordHasher<UserEntity> PasswordHasher { get; set; }
    //public IList<IUserValidator<UserEntity>> UserValidators { get; }
    //public IList<IPasswordValidator<UserEntity>> PasswordValidators { get; }
    public ILookupNormalizer KeyNormalizer { get; set; }
    public IdentityErrorDescriber ErrorDescriber { get; set; }
    public IdentityOptions Options { get; set; }
    public bool SupportsUserAuthenticationTokens { get; }
    public bool SupportsUserAuthenticatorKey { get; }
    public bool SupportsUserTwoFactorRecoveryCodes { get; }
    public bool SupportsUserPassword { get; }
    public ILogger Logger { get; set; }
    public bool SupportsUserSecurityStamp { get; }
    public bool SupportsUserRole { get; }
    public bool SupportsUserLogin { get; }
    public bool SupportsUserEmail { get; }
    public bool SupportsUserPhoneNumber { get; }
    public bool SupportsUserClaim { get; }
    public bool SupportsUserLockout { get; }
    public bool SupportsUserTwoFactor { get; }
    public bool SupportsQueryableUsers { get; }
    //public IQueryable<UserEntity> Users { get; }
    //protected CancellationToken CancellationToken { get; }
    //protected internal IUserStore<UserEntity> Store { get; set; }

    //public static string GetChangeEmailTokenPurpose(string newEmail);
    public Task<IdentityResult> AccessFailedAsync(Guid userId);
    public Task<IdentityResult> AddClaimAsync(Guid userId, Claim claim);
    public Task<IdentityResult> AddClaimsAsync(Guid userId, IEnumerable<Claim> claims);
    public Task<IdentityResult> AddLoginAsync(Guid userId, UserLoginInfo login);
    public Task<IdentityResult> AddPasswordAsync(Guid userId, string password);
    public Task<IdentityResult> AddToRoleAsync(Guid userId, string role);
    public Task<IdentityResult> AddToRolesAsync(Guid userId, IEnumerable<string> roles);
    public Task<IdentityResult> ChangeEmailAsync(Guid userId, string newEmail, string token);
    public Task<IdentityResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    public Task<IdentityResult> ChangePhoneNumberAsync(Guid userId, string phoneNumber, string token);
    public Task<bool> CheckPasswordAsync(Guid userId, string password);
    public Task<IdentityResult> ConfirmEmailAsync(Guid userId, string token);
    public Task<int> CountRecoveryCodesAsync(Guid userId);
    public Task<IdentityResult> CreateAsync(Guid userId);
    public Task<IdentityResult> CreateAsync(Guid userId, string password);
    public Task<byte[]> CreateSecurityTokenAsync(Guid userId);
    public Task<IdentityResult> DeleteAsync(Guid userId);
    public void Dispose();
    //public Task<UserEntity?> FindByEmailAsync(string email);
    //public Task<UserEntity?> FindByIdAsync(string userId);
    //public Task<UserEntity?> FindByLoginAsync(string loginProvider, string providerKey);
    //public Task<UserEntity?> FindByNameAsync(string userName);
    public Task<UserDetail?> FindByEmailAsync(string email);
    public Task<UserDetail?> FindByIdAsync(string userId);
    public Task<UserDetail?> FindByLoginAsync(string loginProvider, string providerKey);
    public Task<UserDetail?> FindByNameAsync(string userName);
    public Task<string> GenerateChangeEmailTokenAsync(Guid userId, string newEmail);
    public Task<string> GenerateChangePhoneNumberTokenAsync(Guid userId, string phoneNumber);
    public Task<string> GenerateConcurrencyStampAsync(Guid userId);
    public Task<string> GenerateEmailConfirmationTokenAsync(Guid userId);
    public string GenerateNewAuthenticatorKey();
    public Task<IEnumerable<string>?> GenerateNewTwoFactorRecoveryCodesAsync(Guid userId, int number);
    public Task<string> GeneratePasswordResetTokenAsync(Guid userId);
    public Task<string> GenerateTwoFactorTokenAsync(Guid userId, string tokenProvider);
    public Task<string> GenerateUserTokenAsync(Guid userId, string tokenProvider, string purpose);
    public Task<int> GetAccessFailedCountAsync(Guid userId);
    public Task<string?> GetAuthenticationTokenAsync(Guid userId, string loginProvider, string tokenName);
    public Task<string?> GetAuthenticatorKeyAsync(Guid userId);
    public Task<IList<Claim>> GetClaimsAsync(Guid userId);
    public Task<string?> GetEmailAsync(Guid userId);
    public Task<bool> GetLockoutEnabledAsync(Guid userId);
    public Task<DateTimeOffset?> GetLockoutEndDateAsync(Guid userId);
    public Task<IList<UserLoginInfo>> GetLoginsAsync(Guid userId);
    public Task<string?> GetPhoneNumberAsync(Guid userId);
    public Task<IList<string>> GetRolesAsync(Guid userId);
    public Task<string> GetSecurityStampAsync(Guid userId);
    public Task<bool> GetTwoFactorEnabledAsync(Guid userId);
    //public Task<UserEntity?> GetUserAsync(ClaimsPrincipal principal);
    public Task<UserDetail?> GetUserAsync(ClaimsPrincipal principal);
    public string? GetUserId(ClaimsPrincipal principal);
    public Task<string> GetUserIdAsync(Guid userId);
    public string? GetUserName(ClaimsPrincipal principal);
    public Task<string?> GetUserNameAsync(Guid userId);
    //public Task<IList<UserEntity>> GetUsersForClaimAsync(Claim claim);
    //public Task<IList<UserEntity>> GetUsersInRoleAsync(string roleName);
    public Task<IList<UserDetail>> GetUsersForClaimAsync(Claim claim);
    public Task<IList<UserDetail>> GetUsersInRoleAsync(string roleName);
    public Task<IList<string>> GetValidTwoFactorProvidersAsync(Guid userId);
    public Task<bool> HasPasswordAsync(Guid userId);
    public Task<bool> IsEmailConfirmedAsync(Guid userId);
    public Task<bool> IsInRoleAsync(Guid userId, string role);
    public Task<bool> IsLockedOutAsync(Guid userId);
    public Task<bool> IsPhoneNumberConfirmedAsync(Guid userId);
    public string? NormalizeEmail(string? email);
    public string? NormalizeName(string? name);
    public Task<IdentityResult> RedeemTwoFactorRecoveryCodeAsync(Guid userId, string code);
    //public void RegisterTokenProvider(string providerName, IUserTwoFactorTokenProvider<UserEntity> provider);
    public Task<IdentityResult> RemoveAuthenticationTokenAsync(Guid userId, string loginProvider, string tokenName);
    public Task<IdentityResult> RemoveClaimAsync(Guid userId, Claim claim);
    public Task<IdentityResult> RemoveClaimsAsync(Guid userId, IEnumerable<Claim> claims);
    public Task<IdentityResult> RemoveFromRoleAsync(Guid userId, string role);
    public Task<IdentityResult> RemoveFromRolesAsync(Guid userId, IEnumerable<string> roles);
    public Task<IdentityResult> RemoveLoginAsync(Guid userId, string loginProvider, string providerKey);
    public Task<IdentityResult> RemovePasswordAsync(Guid userId);
    public Task<IdentityResult> ReplaceClaimAsync(Guid userId, Claim claim, Claim newClaim);
    public Task<IdentityResult> ResetAccessFailedCountAsync(Guid userId);
    public Task<IdentityResult> ResetAuthenticatorKeyAsync(Guid userId);
    public Task<IdentityResult> ResetPasswordAsync(Guid userId, string token, string newPassword);
    public Task<IdentityResult> SetAuthenticationTokenAsync(Guid userId, string loginProvider, string tokenName, string? tokenValue);
    public Task<IdentityResult> SetEmailAsync(Guid userId, string? email);
    public Task<IdentityResult> SetLockoutEnabledAsync(Guid userId, bool enabled);
    public Task<IdentityResult> SetLockoutEndDateAsync(Guid userId, DateTimeOffset? lockoutEnd);
    public Task<IdentityResult> SetPhoneNumberAsync(Guid userId, string? phoneNumber);
    public Task<IdentityResult> SetTwoFactorEnabledAsync(Guid userId, bool enabled);
    public Task<IdentityResult> SetUserNameAsync(Guid userId, string? userName);
    public Task<IdentityResult> UpdateAsync(Guid userId);
    public Task UpdateNormalizedEmailAsync(Guid userId);
    public Task UpdateNormalizedUserNameAsync(Guid userId);
    public Task<IdentityResult> UpdateSecurityStampAsync(Guid userId);
    public Task<bool> VerifyChangePhoneNumberTokenAsync(Guid userId, string token, string phoneNumber);
    public Task<bool> VerifyTwoFactorTokenAsync(Guid userId, string tokenProvider, string token);
    public Task<bool> VerifyUserTokenAsync(Guid userId, string tokenProvider, string purpose, string token);
    //protected string CreateTwoFactorRecoveryCode();
    //protected void Dispose(bool disposing);
    //protected void ThrowIfDisposed();
    //protected Task<IdentityResult> UpdatePasswordHash(Guid userId, string newPassword, bool validatePassword);
    //protected Task<IdentityResult> UpdateUserAsync(Guid userId);
    //protected Task<IdentityResult> ValidatePasswordAsync(Guid userId, string? password);
    //protected Task<IdentityResult> ValidateUserAsync(Guid userId);
    //protected Task<PasswordVerificationResult> VerifyPasswordAsync(IUserPasswordStore<UserEntity> store, Guid userId, string password);
}
