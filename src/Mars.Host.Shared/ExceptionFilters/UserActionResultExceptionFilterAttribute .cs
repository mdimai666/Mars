using System.Text.Json;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars.Host.Shared.ExceptionFilters;

public sealed class UserActionResultExceptionFilterAttribute : ExceptionFilterAttribute
{
    //public const int Status466UserActionException = 466;

    public override void OnException(ExceptionContext context)
    {
        //if (context.Exception is not ExpiredVersionTokenException ex) return;
        if (context.Exception is not UserActionException ex) return;

        var response = UserActionResult.Exception(ex.Message, ex.DetailMessages);
        //var response = new UserActionException(ex.Message);

        context.Result = new ContentResult()
        {
            //StatusCode = Status466UserActionException,
            StatusCode = HttpConstants.UserActionErrorCode466,
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(response)
        };
    }
}
