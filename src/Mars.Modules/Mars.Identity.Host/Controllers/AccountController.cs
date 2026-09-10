using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.Auth;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Mappings.Accounts;
using Mars.Identity.Abstractions.Mappings.UserProfiles;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.Auth;
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
    //TODO: rate limit

    [HttpPost("Login")]
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

    [HttpPost("Logout")]
    [Authorize]
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

    [HttpPost("UploadAvatar")]
    public Task<ActionResult<FileDetailResponse>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        //Guid userId = Guid.Parse(_userManager.GetUserId(HttpContext.User)!);
        //UserSummary user = await _userService.Get(userId, cancellationToken);

        //AppShared.Models.FileEntity ava;

        //ava = _fileService.WriteAvatar(user, file);

        //return new ResponseUploadFile(ava);
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
