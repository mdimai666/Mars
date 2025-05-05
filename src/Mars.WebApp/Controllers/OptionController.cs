using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using Mars.Core.Constants;
using Mars.Core.Models;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Sms;
using Mars.Shared.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

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
    private readonly IMarsEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly IActionHistoryService _actionHistoryService;
    private readonly IMarsAppProvider _marsAppProvider;

    public OptionController(
        IOptionService optionService,
        IMarsEmailSender emailSender,
        ISmsSender smsSender,
        IActionHistoryService actionHistoryService,
        IMarsAppProvider MarsAppProvider)
    {
        _optionService = optionService;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _actionHistoryService = actionHistoryService;
        _marsAppProvider = MarsAppProvider;
    }

    [AllowAnonymous]
    [HttpGet("SysOptions")]
    public SysOptions GetSysOptions()
    {
        return _optionService.SysOption;
    }

    [HttpPut("SysOptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public IActionResult SaveSysOptions(SysOptions val)
    {
        _optionService.SaveOption(val);
        return Ok();
    }

    //[HttpPost(nameof(SaveSmtpSettings))]
    //public async Task<ActionResult<UserActionResult<SmtpSettingsModel>>> SaveSmtpSettings(SmtpSettingsModel val)
    //{
    //    return _optionService.SaveSmtpSettings(val);
    //}

    [HttpPost("SendTestEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<UserActionResult> SendTestEmail(TestMailMessage form)
    {
        return _emailSender.SendTestEmail(form);
    }

    [HttpPost("SendTestSms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<UserActionResult> SendTestSms(SendSmsModelRequest form)
    {
        return _smsSender.SendTestSms(form);
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
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<IActionResult> SaveOption(string optionClass, /*[FromBody] PostOptionValue jsonValue, */CancellationToken cancellationToken)
    {
        ReadResult requestBodyInBytes = await Request.BodyReader.ReadAsync(cancellationToken);
        Request.BodyReader.AdvanceTo(requestBodyInBytes.Buffer.Start, requestBodyInBytes.Buffer.End);
        string body = Encoding.UTF8.GetString(requestBodyInBytes.Buffer.FirstSpan);

        _optionService.SetOptionByClass(optionClass, body);

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

    [HttpGet("AppFrontSettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IEnumerable<AppFrontSettingsCfg>> AppFrontSettings()
    {
        return Ok(_marsAppProvider.Apps.Values.Select(s => s.Configuration));
    }
}
