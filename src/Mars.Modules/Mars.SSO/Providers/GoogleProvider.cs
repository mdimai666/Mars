using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Shared.Contracts.Users;
using Mars.SSO.Mappings;
using Mars.SSO.Utilities;
using Microsoft.IdentityModel.Tokens;

namespace Mars.SSO.Providers;

public class GoogleProvider : GenericOidcProvider
{
    public GoogleProvider(SsoProviderDescriptor descriptor, IHttpClientFactory httpClientFactory, OidcMetadataCache metadataCache)
    : base(descriptor, httpClientFactory, metadataCache) { }

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

        if (token == null) return null;

        throw new NotImplementedException();

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

        return new()
        {
            ExternalKey = claims.GetValueOrDefault("sub") ?? throw new InvalidOperationException("Missing 'sub' claim in Google token"),
            PreferredUserName = claims.GetValueOrDefault("email") ?? Guid.NewGuid().ToString("N"),
            FirstName = claims.GetValueOrDefault("given_name") ?? "Unknown",
            LastName = claims.GetValueOrDefault("family_name"),
            MiddleName = null,
            Email = claims.GetValueOrDefault("email"),
            Roles = Array.Empty<string>(),
            Gender = UserGender.None,
            BirthDate = null,
            PhoneNumber = null,
            Prodvider = _descriptor.ToInfo()
        };
    }
}
