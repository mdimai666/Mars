using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Shared.Contracts.Users;
using Mars.SSO.Mappings;
using Mars.SSO.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Mars.SSO.Providers;

public class GoogleProvider : GenericOidcProvider
{
    public GoogleProvider(SsoProviderDescriptor descriptor,
                        IHttpClientFactory httpClientFactory,
                        OidcMetadataCache metadataCache,
                        ILogger<GoogleProvider> logger)
    : base(descriptor, httpClientFactory, metadataCache, logger) { }

    public override async Task<SsoTokenResponse?> AuthenticateAsync(string code, string redirectUri)
    {
        var config = await _metadataCache.GetConfigurationAsync(_descriptor.Issuer ?? "https://accounts.google.com");
        if (config == null) return null;

        var token = await ExchangeCodeForTokenAsync(config.TokenEndpoint, new Dictionary<string, string>
        {
            ["client_id"] = _descriptor.ClientId!,
            ["client_secret"] = _descriptor.ClientSecret ?? "",
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        });

#if DEBUG
        var jwt = token?.ToString();
#endif

        if (token == null) return null;

        var _OAuth = JsonSerializer.Deserialize<OAuthTokenResponse>(token.Value);

        return new()
        {
            AccessToken = _OAuth.AccessToken,
            OAuthResponse = _OAuth,
            RawResponse = token.Value
        };
    }

    public override async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        var config = await _metadataCache.GetConfigurationAsync(_descriptor.Issuer ?? "https://accounts.google.com");
        if (config == null) return null;

        var tvp = new TokenValidationParameters
        {
            ValidIssuer = _descriptor.Issuer ?? "https://accounts.google.com",
            ValidAudiences = new[] { _descriptor.ClientId },
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            IssuerSigningKeys = config.SigningKeys
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, tvp, out _);
        }
        catch { return null; }
    }

    public override UpsertUserRemoteDataQuery MapToCreateUserQuery(ClaimsPrincipal principal)
    {
        var claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value);
        var externalKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return new()
        {
            ExternalKey = externalKey ?? claims.GetValueOrDefault("sub") ?? throw new InvalidOperationException("Missing 'sub' claim in Google token"),
            PreferredUserName = claims.GetValueOrDefault("email")?.Split('@', 2)[0] ?? Guid.NewGuid().ToString("N"),
            FirstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? claims.GetValueOrDefault("given_name") ?? "Unknown",
            LastName = principal.FindFirstValue(ClaimTypes.Surname) ?? claims.GetValueOrDefault("family_name"),
            MiddleName = null,
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? claims.GetValueOrDefault("email"),
            Roles = DefaultExternalUserRoles,
            Gender = UserGender.None,
            AvatarUrl = claims.GetValueOrDefault("picture"),
            BirthDate = null,
            PhoneNumber = null,
            Prodvider = _descriptor.ToInfo()
        };
    }
}
