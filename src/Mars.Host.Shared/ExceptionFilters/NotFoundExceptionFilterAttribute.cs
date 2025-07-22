using Mars.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars.Host.Shared.ExceptionFilters;

public sealed class NotFoundExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not NotFoundException ex) return;

        //context.Result = new NotFoundResult();

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Title = "Not Found",
            Status = 404,
            Detail = ex.Message, // Сообщение из исключения, если нужно
            Instance = context.HttpContext.Request.Path,
        };

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = 404
        };
        context.ExceptionHandled = true;
    }
}
