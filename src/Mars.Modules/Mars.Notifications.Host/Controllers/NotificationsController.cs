using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Core.Constants;
using Mars.Notifications.Abstractions;
using Mars.Notifications.Contracts;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Notifications.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class NotificationsController : ControllerBase
{
    private readonly IMarsEmailSender _emailSender;
    private readonly ISmsSender _smsSender;

    public NotificationsController(
        IMarsEmailSender emailSender,
        ISmsSender smsSender)
    {
        _emailSender = emailSender;
        _smsSender = smsSender;
    }

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
}
