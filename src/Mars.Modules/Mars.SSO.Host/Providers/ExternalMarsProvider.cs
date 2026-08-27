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

internal class ExternalMarsProvider : GenericOidcProvider
{
    public ExternalMarsProvider(SsoProviderDescriptor descriptor,
                                IHttpClientFactory httpClientFactory,
                                IServiceProvider serviceProvider,
                                OidcMetadataCache metadataCache,
                                ILogger<ExternalMarsProvider> logger)
    : base(descriptor, httpClientFactory, metadataCache, logger)
    {
    }

    public override async Task<SsoTokenResponse?> AuthenticateAsync(string code, string redirectUri/*, bool writeCookies*/)
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
        var config = await _metadataCache.GetConfigurationAsync(_descriptor.Issuer!);
        if (config == null) return null;

        var tvp = new TokenValidationParameters
        {
            ValidIssuer = _descriptor.Issuer,
            //ValidAudiences = ["mars"],
            ValidateIssuer = true,
            ValidateAudience = !true,
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
        var roles = principal.Claims.Where(s => s.Type == ClaimTypes.Role).Select(s => s.Value).ToArray() ?? [];

        return new()
        {
            ExternalKey = externalKey ?? throw new InvalidOperationException($"Missing '{ClaimTypes.NameIdentifier}' claim in Mars token"),
            PreferredUserName = claims[ClaimTypes.Name],
            FirstName = claims[ClaimTypes.GivenName],
            LastName = claims[ClaimTypes.Surname],
            MiddleName = null,
            Email = claims[ClaimTypes.Email],
            Roles = roles.Any() ? roles : DefaultExternalUserRoles,
            BirthDate = null,
            Gender = UserGender.None,
            PhoneNumber = null,
            Prodvider = _descriptor.ToInfo()
        };
    }

}
