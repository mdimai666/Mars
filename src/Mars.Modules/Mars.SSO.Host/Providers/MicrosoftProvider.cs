using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.Users;
using Mars.SSO.Mappings;
using Mars.SSO.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Mars.SSO.Providers;

// Microsoft (Azure AD) provider using OIDC
/*
 1. есть проблема с id_token и access_token. Он выдвется но работает как то хитро. С access_token нельзя ходить в майкрософт, а с id_token можно.
 2. Еще issuer другой прилетает
 */
public class MicrosoftProvider : GenericOidcProvider
{
    public MicrosoftProvider(SsoProviderDescriptor descriptor,
                            IHttpClientFactory httpClientFactory,
                            OidcMetadataCache metadataCache,
                            ILogger<MicrosoftProvider> logger)
    : base(descriptor, httpClientFactory, metadataCache, logger) { }

    public override async Task<SsoTokenResponse?> AuthenticateAsync(string code, string redirectUri)
    {
        var config = await _metadataCache.GetConfigurationAsync(_descriptor.Issuer!);
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

        //if (!token.Value.TryGetProperty("id_token", out var idTokenProp)) return null;
        //var idToken = idTokenProp.GetString();
        //var handler = new JwtSecurityTokenHandler();
        //var jwt = handler.ReadJwtToken(idToken!);

        //return new SsoUserInfo
        //{
        //    InternalId = Guid.Empty,
        //    ExternalId = jwt.Subject,
        //    Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
        //    Name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
        //    Provider = Name
        //};
    }

    public override async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        var config = await _metadataCache.GetConfigurationAsync(_descriptor.Issuer!);
        if (config == null) return null;

        var tvp = new TokenValidationParameters
        {
            ValidIssuer = _descriptor.Issuer,
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
        var claims = principal.Claims.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => string.Join(",", g.Select(c => c.Value)));

        var externalKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string[] roles = DefaultExternalUserRoles;

        return new()
        {
            ExternalKey = externalKey ?? claims.GetValueOrDefault("sub") ?? throw new InvalidOperationException("Missing 'sub' claim in Keycloak token"),
            PreferredUserName = claims.GetValueOrDefault("preferred_username")//TODO: сделать nullable
                        ?? claims.GetValueOrDefault("email")
                        ?? Guid.NewGuid().ToString("N"),
            FirstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? claims.GetValueOrDefault("given_name") ?? "Unknown",
            LastName = principal.FindFirstValue(ClaimTypes.Surname) ?? claims.GetValueOrDefault("family_name"),
            MiddleName = null,
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? claims.GetValueOrDefault("email"),
            Roles = roles,
            BirthDate = DateTime.TryParse(claims.GetValueOrDefault("birthdate"), out var birth)
                        ? birth : null,
            Gender = claims.GetValueOrDefault("gender")?.ToLower() switch
            {
                "male" => UserGender.Male,
                "female" => UserGender.Female,
                _ => UserGender.None,
            },
            PhoneNumber = PhoneUtil.GetNormalizedOrNull(claims.GetValueOrDefault("phone_number")),
            Prodvider = _descriptor.ToInfo()
        };
    }
}
