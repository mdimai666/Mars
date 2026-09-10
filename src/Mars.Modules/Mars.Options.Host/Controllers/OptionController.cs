using System.Net.Mime;
using System.Text.Json.Nodes;
using Mars.Contracts.Common;
using Mars.Core.Constants;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Server.Contracts.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Options.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[OptionNotRegisteredExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class OptionController : ControllerBase
{
    private readonly IOptionService _optionService;

    public OptionController(IOptionService optionService)
    {
        _optionService = optionService;
    }

    [AllowAnonymous]
    [HttpGet("SiteSettings")]
    public SiteSettings GetSiteSettings()
    {
        return _optionService.GetOption<SiteSettings>();
    }

    [HttpPut("SiteSettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public IActionResult SaveSiteSettings(SiteSettings val)
    {
        _optionService.SaveOption(val);
        return Ok();
    }

    [HttpGet("Option/{optionClass}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostOptionValue))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public ActionResult<object> GetOption(string optionClass)
    {
        var opt = _optionService.GetOptionByClass(optionClass);
        return Ok(opt);
    }

    public class PostOptionValue
    {
        public string Value { get; set; } = "prop";
    }

    [HttpPut("Option/{optionClass}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostOptionValue))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveOption(string optionClass, [FromBody] JsonNode jsonValue, CancellationToken cancellationToken)
    {
        _optionService.SetOptionByClass(optionClass, jsonValue.ToString());

        return Ok();
    }

    //http://localhost:5003/api/option/setlanguage?culture=en&returnUrl=/
    [AllowAnonymous]
    [HttpGet(nameof(SetLanguage))]
    [HttpPost(nameof(SetLanguage))]
    public IActionResult SetLanguage([FromQuery] string culture, [FromQuery] string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );

        return LocalRedirect(returnUrl);
    }
}
