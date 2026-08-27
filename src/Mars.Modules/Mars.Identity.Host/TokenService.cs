using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Mars.Core.Exceptions;
using Mars.Host.Models;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Mars.Host.Services;

public class TokenService : ITokenService
{
    readonly JwtSettings _jwtSettings;
    private readonly IKeyMaterialService _keys;
    private readonly IOptionService _optionService;

    public int ExpiryInMinutes => _jwtSettings.ExpiryInMinutes;
    public int ExpiryInSeconds => _jwtSettings.ExpiryInMinutes * 60;

    string _validIssuer;

    public TokenService(IOptions<JwtSettings> jwtSettings, IKeyMaterialService keys, IOptionService optionService)
    {
        _jwtSettings = jwtSettings.Value;
        _keys = keys;
        _optionService = optionService;
        _validIssuer = _optionService.SysOption.SiteUrl.TrimEnd('/');

        _optionService.OnOptionUpdate += OptionService_OnOptionUpdate;
    }

    private void OptionService_OnOptionUpdate(object obj)
    {
        if (obj is SysOptions sysOptions)
            _validIssuer = sysOptions.SiteUrl;
    }

    public SigningCredentials GetSigningCredentials()
        => _keys.GetSigningCredentials();

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

        if (!string.IsNullOrEmpty(user.AvatarUrl))
            claims.Add(new("picture", user.AvatarUrl));

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    public JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
    {
        var tokenOptions = new JwtSecurityToken(
            issuer: _validIssuer,
            audience: _jwtSettings.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtSettings.ExpiryInMinutes)),
            signingCredentials: signingCredentials);
        return tokenOptions;
    }

    public long JwtExpireUnixSeconds()
    {
        var expireDate = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtSettings.ExpiryInMinutes));
        return JwtExpireDateTimeToUnixSeconds(expireDate);
    }

    public async Task<string> CreateAccessToken(Guid userId, IUserRepository userRepository, CancellationToken cancellationToken)
    {
        var userInfo = await userRepository.GetAuthorizedUserInformation(userId, cancellationToken) ?? throw new NotFoundException();

        var signingCredentials = GetSigningCredentials();
        var claims = GetClaims(userInfo);
        var tokenOptions = GenerateTokenOptions(signingCredentials, claims);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(tokenOptions);

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

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _validIssuer,
            ValidAudience = _jwtSettings.ValidAudience,
            IssuerSigningKey = _keys.SigningKey,
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
            IssuerSigningKey = _keys.SigningKey,
            ValidateLifetime = false,
            ValidIssuer = _validIssuer,
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

    public static long JwtExpireDateTimeToUnixSeconds(DateTime expire)
    {
        return new DateTimeOffset(expire).ToUnixTimeSeconds();
    }

}
