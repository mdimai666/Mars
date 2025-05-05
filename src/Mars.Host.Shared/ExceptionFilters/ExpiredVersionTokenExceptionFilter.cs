using System.Text.Json;
using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars.Host.Shared.ExceptionFilters;

public sealed class ExpiredVersionTokenExceptionFilter : IExceptionFilter
{
    public const int Status460ConcurrencyTokenException = 460;

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ExpiredVersionTokenException ex) return;

        //var response = new
        //{
        //    Title = ex.Message,
        //    Status = Status460ConcurrencyTokenException,
        //    Detail = context.Exception.InnerException?.Message ?? "",
        //};
        var response = UserActionResult.Exception(ex);

        context.Result = new ContentResult()
        {
            StatusCode = Status460ConcurrencyTokenException,
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(response)
        };
    }
}
