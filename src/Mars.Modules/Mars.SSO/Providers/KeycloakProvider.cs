using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.Users;
using Mars.SSO.Mappings;
using Mars.SSO.Utilities;
using Microsoft.IdentityModel.Tokens;

namespace Mars.SSO.Providers;

internal class KeycloakProvider : GenericOidcProvider
{
    public KeycloakProvider(SsoProviderDescriptor descriptor,
                                IHttpClientFactory httpClientFactory,
                                IServiceProvider serviceProvider,
                                OidcMetadataCache metadataCache)
    : base(descriptor, httpClientFactory, metadataCache)
    {
    }

    public override async Task<SsoTokenResponse?> AuthenticateAsync(string code, string redirectUri/*, bool writeCookies*/)
    {
        Console.WriteLine("KeycloakProvider:AuthenticateAsync");
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
        Console.WriteLine("KeycloakProvider:ValidateTokenAsync");
        var config = await _metadataCache.GetConfigurationAsync(_descriptor.Issuer!);
        if (config == null) return null;

        var tvp = new TokenValidationParameters
        {
            ValidIssuer = _descriptor.Issuer,
            ValidAudiences = ["account", _descriptor.ClientId],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            IssuerSigningKeys = config.SigningKeys
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, tvp, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public override UpsertUserRemoteDataQuery MapToCreateUserQuery(ClaimsPrincipal principal)
    {
        var claims = principal.Claims.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => string.Join(",", g.Select(c => c.Value)));

        var externalKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var roles = GetKeycloakRoles(principal);

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

    public static IReadOnlyCollection<string> GetKeycloakRoles(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirstValue("realm_access");
        if (string.IsNullOrEmpty(claim)) return Array.Empty<string>();

        using var doc = JsonDocument.Parse(claim);
        if (doc.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
        {
            return rolesElement.EnumerateArray().Select(r => r.GetString()!).Where(r => !string.IsNullOrEmpty(r)).ToArray();
        }

        return Array.Empty<string>();
    }
}
