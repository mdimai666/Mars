using Mars.Core.Exceptions;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.Auth;
using Mars.Identity.Abstractions.Dto.Profile;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.Users.UserProfiles;
using Mars.Media.Abstractions.Services;
using Mars.Media.Contracts;
using Mars.Media.Contracts.Options;
using Mars.Server.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Mars.Identity.Host.Services;

internal class AccountsService : IAccountsService
{
    private static readonly HashSet<string> AllowedAvatarExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp",
    };

    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly IUserTypeRepository _userTypeRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IImageProcessor _imageProcessor;

    public AccountsService(
        UserManager<UserEntity> userManager,
        SignInManager<UserEntity> signInManager,
        IHttpContextAccessor httpContextAccessor,
        ITokenService tokenService,
        IUserRepository userRepository,
        IUserTypeRepository userTypeRepository,
        IFileStorage fileStorage,
        IImageProcessor imageProcessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;

        _tokenService = tokenService;
        _userRepository = userRepository;
        _userTypeRepository = userTypeRepository;
        _fileStorage = fileStorage;
        _imageProcessor = imageProcessor;
    }

    public async Task<AuthResultDto> Login(AuthCredentialsDto authCredentials, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(authCredentials.Login) ?? await _userManager.FindByEmailAsync(authCredentials.Login);

        if (user == null)
        {
            return AuthResultDto.InvalidDataResponse();
        }

        var signInResult = await _signInManager.PasswordSignInAsync(user, authCredentials.Password, isPersistent: true, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return AuthResultDto.ErrorResponse("Учётная запись временно заблокирована из-за неудачных попыток входа");
        }

        if (!signInResult.Succeeded)
        {
            return AuthResultDto.InvalidDataResponse();
        }

        var token = await _tokenService.CreateAccessToken(user.Id, _userRepository, cancellationToken);

        return new AuthResultDto
        {
            Token = token,
            ExpiresIn = _tokenService.JwtExpireUnixSeconds(),
            ErrorMessage = null,
            RefreshToken = null
        };
    }

    public async Task<AuthResultDto> LoginForce(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return AuthResultDto.InvalidDataResponse();
        }

        var token = await _tokenService.CreateAccessToken(user.Id, _userRepository, cancellationToken);

        return new AuthResultDto
        {
            Token = token,
            ExpiresIn = _tokenService.JwtExpireUnixSeconds(),
            ErrorMessage = null,
            RefreshToken = null
        };
    }

    public async Task<string?> FindPrefererUserName(string userInfoPrefererUsername)
    {
        string prefererName = userInfoPrefererUsername;
        int tryCountLeast = 6;

        var existWithThisPrefererName = await _userManager.FindByNameAsync(prefererName);

        while (existWithThisPrefererName != null && tryCountLeast > 0)
        {
            var postfixNumber = Random.Shared.Next(1001, 10000).ToString();
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

    public async Task<RegistrationResponseDto> RegisterUser(UserForRegistrationQuery userData, CancellationToken cancellationToken)
    {
        // username — local-part почты; при коллизии FindPrefererUserName добавляет цифровой суффикс
        var userName = await FindPrefererUserName(userData.Email.Split('@', 2)[0])
                       ?? Guid.NewGuid().ToString("N");

        var user = new UserEntity
        {
            UserName = userName,
            Email = userData.Email,
            EmailConfirmed = true,
            LockoutEnabled = true,
            FirstName = userData.FirstName ?? userData.Email.Split('@', 2)[0],
            LastName = userData.LastName ?? "",
        };

        return await RegisterUser(user, userData.Password, cancellationToken);
    }

    private async Task<RegistrationResponseDto> RegisterUser(UserEntity user, string password, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));
        if (user.UserTypeId == Guid.Empty)
        {
            var userType = await _userTypeRepository.GetByName("default", cancellationToken);
            user.UserTypeId = userType?.Id ?? Guid.Empty;
        }
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

    public Task<UserProfileDto?> GetProfile(Guid userId, CancellationToken cancellationToken)
    {
        return _userRepository.UserProfile(userId, cancellationToken);
    }

    public Task Logout()
    {
        return _signInManager.SignOutAsync();
    }

    public async Task<Guid?> ValidateUserCredentials(string username, string password, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(username);

        if (user == null) return null;

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        return signInResult.Succeeded ? user.Id : null;
    }

    public async Task<AvatarUploadResponse> UploadAvatar(Guid userId, Stream image, string fileName, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(fileName);
        if (!AllowedAvatarExtensions.Contains(ext) || !_imageProcessor.IsSupportImageExt(ext))
            throw new UserActionException($"Неподдерживаемый формат изображения: {ext}");

        var dir = $"Users/{userId}";
        if (!_fileStorage.DirectoryExists(dir))
            _fileStorage.CreateDirectory(dir);

        // имя файла стабильное — версией в query сбрасываем кэш браузера
        var version = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await WriteResizedAsync(image, $"{dir}/avatar.webp", 256, cancellationToken);
        image.Position = 0;
        await WriteResizedAsync(image, $"{dir}/avatar-64.webp", 64, cancellationToken);

        return new AvatarUploadResponse(
            $"/upload/{dir}/avatar.webp?v={version}",
            $"/upload/{dir}/avatar-64.webp?v={version}");
    }

    async Task WriteResizedAsync(Stream input, string filePath, int size, CancellationToken cancellationToken)
    {
        input.Position = 0;
        var config = new ImageConverConfig
        {
            Width = size,
            Height = size,
            Compression = ImagePreviewSizeConfig.EncoderCompression.Lossy,
            ResizeMode = ImagePreviewSizeConfig.CropScaleMode.Crop,
        };

        using var output = new MemoryStream();
        _imageProcessor.ProcessImage(input, output, config);
        output.Position = 0;
        await _fileStorage.WriteAsync(filePath, output, cancellationToken);
    }

}
