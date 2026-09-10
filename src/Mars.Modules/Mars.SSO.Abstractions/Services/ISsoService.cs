using System.Security.Claims;
using Mars.SSO.Abstractions.Interfaces;
using Mars.SSO.Contracts.Dto;

namespace Mars.SSO.Abstractions.Services;

public interface ISsoService
{
    IEnumerable<SsoProviderDescriptor> Providers { get; }
    ISsoProvider? GetProvider(string name);
    Task<SsoUserInfo?> AuthenticateAsync(string providerName, string code, string redirectUri);
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    IEnumerable<ISsoProvider> CreateProviderList();
    bool TryValidateIssuer(string issuer, out SsoProviderDescriptor? ssoProviderDescriptor);
}
