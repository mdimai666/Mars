using System.Security.Claims;
using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.SSO.Interfaces;

namespace Mars.Host.Shared.SSO.Services;

public interface ISsoService
{
    IEnumerable<SsoProviderDescriptor> Providers { get; }
    ISsoProvider? GetProvider(string name);
    Task<SsoUserInfo?> AuthenticateAsync(string providerName, string code, string redirectUri);
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    IEnumerable<ISsoProvider> CreateProviderList();
    bool TryValidateIssuer(string issuer, out SsoProviderDescriptor? ssoProviderDescriptor);
}
