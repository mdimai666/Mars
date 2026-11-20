using Mars.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars.Server.Abstractions.ExceptionFilters;

public sealed class ForbiddenExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not ForbiddenException ex) return;

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            Title = "Forbidden",
            Status = 403,
            Detail = ex.Message,
            Instance = context.HttpContext.Request.Path,
        };

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = 403
        };
        context.ExceptionHandled = true;
    }
}
