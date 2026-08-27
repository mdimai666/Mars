using System.Text.Json;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars.Host.Shared.ExceptionFilters;

public sealed class AllExceptionCatchToUserActionResultFilterAttribute : ExceptionFilterAttribute
{
    public new int Order = 999;

    public override void OnException(ExceptionContext context)
    {
        var response = UserActionResult.Exception(context.Exception);

        context.Result = new ContentResult()
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(response)
        };
    }
}
