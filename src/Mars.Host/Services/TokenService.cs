using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mars.Core.Exceptions;
using Mars.Host.Models;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Mars.Host.Services;

public class TokenService : ITokenService
{
    readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;

        if (!CheckSecretKeyLength())
        {
            throw new ApplicationException("Jwt Secret too short. Require min 32 symbols");
        }
    }

    public SigningCredentials GetSigningCredentials()
    {
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey);
        var secret = new SymmetricSecurityKey(key);
        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }
    public List<Claim> GetClaims(AuthorizedUserInformationDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user?.Email??""),
            // https://learn.microsoft.com/ru-ru/dotnet/api/microsoft.aspnetcore.identity.identityuser-1.securitystamp?view=aspnetcore-8.0#definition
            new("AspNet.Identity.SecurityStamp", user.SecurityStamp),
            new(ClaimTypes.GivenName, user.FirstName??""),
            new(ClaimTypes.Surname, user.LastName??""),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    public JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
    {
        var tokenOptions = new JwtSecurityToken(
            issuer: _jwtSettings.ValidIssuer,
            audience: _jwtSettings.ValidAudience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_jwtSettings.ExpiryInMinutes)),
            signingCredentials: signingCredentials);
        return tokenOptions;
    }

    public long JwtExpireUnixSeconds()
    {
        var expireDate = DateTime.Now.AddMinutes(Convert.ToDouble(_jwtSettings.ExpiryInMinutes));
        return JwtExpireDateTimeToUnixSeconds(expireDate);
    }

    public async Task<string> CreateToken(Guid userId, IUserRepository userRepository, CancellationToken cancellationToken)
    {
        var userInfo = await userRepository.GetAuthorizedUserInformation(userId, cancellationToken) ?? throw new NotFoundException();

        var signingCredentials = GetSigningCredentials();
        var claims = GetClaims(userInfo);
        var tokenOptions = GenerateTokenOptions(signingCredentials, claims);
        var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        return token;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey));

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.ValidIssuer,
            ValidAudience = _jwtSettings.ValidAudience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey)),
            ValidateLifetime = false,
            ValidIssuer = _jwtSettings.ValidIssuer,
            ValidAudience = _jwtSettings.ValidAudience,
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
            StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }

    public T? JwtDecode<T>(string token, string? secret = null, bool verify = true)
    {
        var handler = new JwtSecurityTokenHandler();
        string json;

        if (secret is null)
        {
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var validations = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = false,
                ValidateIssuer = false,
                ValidateAudience = false
            };

            //if (verify)
            //{
            //    var claims = handler.ValidateToken(token, validations, out var _);
            //}

            json = JsonSerializer.Serialize(jwtSecurityToken.Payload);
        }
        else
        {
            //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var bb = Encoding.UTF8.GetBytes(secret);
            //var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hmac = new HMACSHA256(bb);
            var key = new SymmetricSecurityKey(hmac.Key);
            var validations = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = false
            };

            //if (verify && bb.Length >= 128) // для legacy совместимости
            //{
            //    var claims = handler.ValidateToken(token, validations, out var _);
            //}
            var jwtSecurityToken = handler.ReadJwtToken(token);

            json = JsonSerializer.Serialize(jwtSecurityToken.Payload);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception)
        {
            return default;
        }
    }

    public string JwtEncode(Dictionary<string, object> payload)
    {
        return JwtEncode(payload, _jwtSettings.SecurityKey);
    }

    public string JwtEncode(Dictionary<string, object> payload, string secret)
    {

        Claim[] claims = payload.Select(s => new Claim(s.Key, s.Value.ToString())).ToArray();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken("issuer", "audience", claims, expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes), signingCredentials: credentials);
        var encodedToken = new JwtSecurityTokenHandler().WriteToken(token);

        return encodedToken;
    }

    public static long JwtExpireDateTimeToUnixSeconds(DateTime expire)
    {
        return new DateTimeOffset(expire).ToUnixTimeSeconds();
    }

    private bool CheckSecretKeyLength()
    {
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey);

        int length = key.Length;

        return length * 8 >= 256;
    }

    public static void ThrowIfJwtProblem(string jwtKey)
    {
        ArgumentNullException.ThrowIfNull(jwtKey, nameof(jwtKey));

        int length = jwtKey.Length;

        if (length * 8 <= 256)
        {
            throw new ArgumentException("Jwt Secret too short. Require min 32 symbols. \nsee: appsettings.json");
        }
    }
}
