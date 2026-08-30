using System.Text.Json;
using Mars.Contracts.Common;
using Mars.Options.Abstractions.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars.Options.Host.Controllers;

public sealed class OptionNotRegisteredExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not OptionNotRegisteredException ex) return;

        var response = UserActionResult.Exception(ex.Message, null);

        context.Result = new ContentResult()
        {
            StatusCode = StatusCodes.Status404NotFound,
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(response)
        };
    }
}
