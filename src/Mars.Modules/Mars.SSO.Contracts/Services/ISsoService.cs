using System.Security.Claims;
using Mars.SSO.Contracts.Dto;
using Mars.SSO.Contracts.Interfaces;

namespace Mars.SSO.Contracts.Services;

public interface ISsoService
{
    IEnumerable<SsoProviderDescriptor> Providers { get; }
    ISsoProvider? GetProvider(string name);
    Task<SsoUserInfo?> AuthenticateAsync(string providerName, string code, string redirectUri);
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    IEnumerable<ISsoProvider> CreateProviderList();
    bool TryValidateIssuer(string issuer, out SsoProviderDescriptor? ssoProviderDescriptor);
}
