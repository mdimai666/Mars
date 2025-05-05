using System.Text.Json;
using Mars.Host.Shared.Exceptions;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars.Host.Shared.ExceptionFilters;

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
