using System.IdentityModel.Tokens.Jwt;
using Mars.Host.Data.Entities;
using Mars.Host.Models;
using Mars.Host.Shared.Dto.Auth;
using Mars.Host.Shared.Dto.Profile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Mars.Host.Services;

public class AccountsService
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly ITokenService _tokenService;

    public AccountsService(
        UserManager<UserEntity> userManager,
        SignInManager<UserEntity> signInManager,
        IHttpContextAccessor httpContextAccessor,
        IOptions<JwtSettings> jwtSettings,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;

        if (jwtSettings.Value.SecurityKey == null)
        {
            throw new ApplicationException("Jwt section not field JWTSettings");
        }

        _tokenService = tokenService;
    }

    public async Task<AuthResultDto> Login(AuthCreditionalsDto authCreditionals)
    {
        var user = await _userManager.FindByNameAsync(authCreditionals.Login);

        if (user == null || !await _userManager.CheckPasswordAsync(user, authCreditionals.Password))
        {
            return AuthResultDto.InvalidDataResponse();
        }

        var signingCredentials = _tokenService.GetSigningCredentials();
        var claims = await _tokenService.GetClaimsAsync(user, _userManager);
        var tokenOptions = _tokenService.GenerateTokenOptions(signingCredentials, claims);
        var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        var refreshToken = _tokenService.GenerateRefreshToken();

        if (true)
        {
            await _signInManager.SignInAsync(user, true);

        }

        return new AuthResultDto
        {
            Token = token,
            ExpiresIn = TokenService.JwtExpireDateTimeToUnixSeconds(tokenOptions.ValidTo),
            ErrorMessage = null,
            RefreshToken = null
        };
    }

    public async Task<AuthResultDto> LoginForce(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return AuthResultDto.InvalidDataResponse();
        }

        var signingCredentials = _tokenService.GetSigningCredentials();
        var claims = await _tokenService.GetClaimsAsync(user, _userManager);
        var tokenOptions = _tokenService.GenerateTokenOptions(signingCredentials, claims);
        var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

        return new AuthResultDto
        {
            Token = token,
            ExpiresIn = TokenService.JwtExpireDateTimeToUnixSeconds(tokenOptions.ValidTo),
            ErrorMessage = null,
            RefreshToken = null
        };
    }

    public async Task<string?> FindPrefererUserName(string userInfoPrefererUsername)
    {
        string prefererName = userInfoPrefererUsername;
        int tryCountLeast = 6;
        Random rnd = new Random();

        var existWithThisPrefererName = await _userManager.FindByNameAsync(prefererName);

        while (existWithThisPrefererName != null && tryCountLeast > 0)
        {
            var postfixNumber = rnd.Next(1001, 10000).ToString();
            --tryCountLeast;
            prefererName = userInfoPrefererUsername + postfixNumber;
            existWithThisPrefererName = await _userManager.FindByNameAsync(prefererName);
        }
        if (tryCountLeast == 0)
        {
            return null;
        }
        return prefererName;
    }

    public async Task<RegistrationResponseDto> RegisterUser(UserForRegistrationQuery userData)
    {
        var user = new UserEntity
        {
            UserName = userData.Email,
            Email = userData.Email,
            EmailConfirmed = true,
            LockoutEnabled = true,
            FirstName = userData.FirstName ?? userData.Email.Split('@', 2)[0],
            LastName = userData.LastName ?? "",
        };

        return await RegisterUser(user, userData.Password);
    }
    public async Task<RegistrationResponseDto> RegisterUser(UserEntity user, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

        IdentityResult result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(s => s.Description).ToList();

            for (int i = 0; i < errors.Count(); i++)
            {
                if (errors[i].Contains("already taken"))
                {
                    errors[i] = $"Пользователь с такой почтой уже существует";
                }
            }

            return new RegistrationResponseDto
            {
                IsSuccessfulRegistration = false,
                Errors = errors,
                Code = StatusCodes.Status400BadRequest,
            };
        }

        //await _userManager.AddToRoleAsync(user, "Viewer");

        return new RegistrationResponseDto
        {
            IsSuccessfulRegistration = true,
            Code = StatusCodes.Status201Created
        };
    }

    public Task<ProfileDto?> GetProfile()
    {
        //var _user = _httpContextAccessor.HttpContext.User;

        //if (!_user.Identity.IsAuthenticated) return null;

        //var user = await _userManager.GetUserAsync(_user);

        //if (user == null) return null;

        //return new ProfileDto(user);
        throw new NotImplementedException();
    }

}
