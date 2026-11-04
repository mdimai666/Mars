using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.Auth;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Mappings.Accounts;
using Mars.Identity.Abstractions.Mappings.UserProfiles;
using Mars.Identity.Abstractions.Mappings.Users;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.Auth;
using Mars.Identity.Contracts.Options;
using Mars.Identity.Contracts.Users;
using Mars.Identity.Contracts.Users.UserProfiles;
using Mars.Media.Abstractions.Services;
using Mars.Media.Contracts.Files;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Server.Contracts.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Mars.Identity.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class AccountController : ControllerBase
{
    protected readonly UserManager<UserEntity> _userManager;
    protected readonly IAccountsService _accountsService;
    protected readonly IUserService _userService;
    protected readonly IFileService _fileService;
    protected readonly IOptionService _optionService;
    private readonly IRequestContext _requestContext;

    public AccountController(
        UserManager<UserEntity> userManager,
        IAccountsService accountsService,
        IUserService userService,
        IFileService fileService,
        IOptionService optionService,
        IRequestContext requestContext)
    {
        _userManager = userManager;
        _accountsService = accountsService;
        _userService = userService;
        _fileService = fileService;
        _optionService = optionService;
        _requestContext = requestContext;
    }

    //https://code-maze.com/blazor-webassembly-authentication-aspnetcore-identity/
    //TODO: use [ValidateAntiForgeryToken]

    [HttpPost("Login")]
    [EnableRateLimiting(AuthProtectionOption.RateLimitPolicyName)]
    [ProducesResponseType(typeof(AuthResultResponse), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultResponse>> Login([FromBody] AuthCredentialsDto authCredentials, CancellationToken cancellationToken)
    {
        if (authCredentials == null || !ModelState.IsValid)
            return BadRequest();

        var result = await _accountsService.Login(authCredentials, cancellationToken);

        if (result.IsAuthSuccessful)
        {
            return Ok(result.ToResponse());
        }

        return Unauthorized(result.ToResponse());
    }

    // без [Authorize]: выход должен снимать cookie даже с протухшим bearer; SignOutAsync безопасен
    [HttpPost("Logout")]
    public Task Logout()
    {
        return _accountsService.Logout();
    }

    [HttpPost("RegisterUser")]
    [ProducesResponseType(typeof(RegistrationResultResponse), StatusCodes.Status201Created)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegistrationResultResponse>> RegisterUser([FromBody] UserForRegistrationRequest userData, CancellationToken cancellationToken)
    {
        if (_optionService.GetOption<SiteSettings>().AllowUsersSelfRegister == false)
        {
            return new RegistrationResultResponse
            {
                IsSuccessfulRegistration = false,
                Errors = ["Регистрация запрещена"],
                Code = HttpConstants.UserActionErrorCode466
            };
        }

        if (userData == null || !ModelState.IsValid)
            return BadRequest();

        var result = await _accountsService.RegisterUser(userData.ToQuery(), cancellationToken);

        if (!result.IsSuccessfulRegistration)
        {
            return BadRequest(result.ToResponse());
        }
        else
        {
            return StatusCode(201, result.ToResponse());
        }
    }

    [HttpGet("Profile")]
    [Authorize]
    public async Task<UserProfileResponse?> Profile(CancellationToken cancellationToken)
    {
        var userId = _requestContext.User.Id;
        return (await _accountsService.GetProfile(userId, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet("Profile/EditModel")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileEditViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<UserProfileEditViewModel> ProfileEditModel(CancellationToken cancellationToken)
    {
        var userId = _requestContext.User.Id;
        return await _userService.GetProfileEditModel(userId, cancellationToken);
    }

    [HttpPut("Profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<UserDetailResponse> UpdateProfile([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = _requestContext.User.Id;
        return (await _userService.UpdateProfile(userId, request, cancellationToken)).ToResponse();
    }

    /// <summary>
    /// Загрузка аватара: пишет полный (256×256) и миниатюру (64×64) в upload/Users/{id}/,
    /// возвращает их URL. AvatarUrl в профиле сохраняется отдельным обновлением (PUT Profile).
    /// userId можно передать только администратору (загрузка аватара другого пользователя).
    /// </summary>
    [HttpPost("Avatar")]
    [Authorize]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(AvatarUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<AvatarUploadResponse> UploadAvatar(IFormFile file, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new UserActionException("Файл пуст");

        var currentUserId = _requestContext.User.Id;
        var targetUserId = userId ?? currentUserId;
        if (targetUserId != currentUserId && !User.IsInRole("Admin"))
            throw new UserActionException("Загружать аватар другого пользователя может только администратор");

        await using var stream = file.OpenReadStream();
        return await _accountsService.UploadAvatar(targetUserId, stream, file.FileName, cancellationToken);
    }

    string html_redirect(string link) => html(@$"
    <h1>redirect...</h1>
    <script>
        location = ""{link}"";
    </script>");

    string html(string body) => @$"<html lang=""ru"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{_optionService.GetOption<SiteSettings>().SiteName}</title>
</head>
<body class="""" >
    {body}
</body>
</html>
";

}
