using Mars.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars.Host.Shared.ExceptionFilters;

public sealed class NotFoundExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not NotFoundException ex) return;

        context.Result = new NotFoundResult();
    }
}
