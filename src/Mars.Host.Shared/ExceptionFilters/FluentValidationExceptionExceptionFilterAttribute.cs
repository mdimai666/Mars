using System.Text.Json;
using Mars.Core.Exceptions;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mars.Host.Shared.ExceptionFilters;

/// <summary>
/// <list type="bullet">
/// <item>catch <see cref="FluentValidation.ValidationException"/></item>
/// <item>catch <see cref="MarsValidationException"/></item>
/// <item>produce BadRequest 400</item>
/// </list>
/// </summary>
public sealed class FluentValidationExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        IDictionary<string, string[]> validateResults;

        if (context.Exception is FluentValidation.ValidationException ex1)
        {
            validateResults = ex1.Errors
                            .GroupBy(s => s.PropertyName)
                            .ToDictionary(s => s.Key, s => s.Select(v => v.ErrorMessage).ToArray());
        }
        else if (context.Exception is MarsValidationException ex2)
        {
            validateResults = ex2.Errors;
        }
        else
        {
            return;
        }

        //var response = new MarsValidationException(
        //            ex.Message,
        //            validateResults,
        //            ex.InnerException);

        var response = new ValidationProblemDetails()
        {
            Title = AppRes.ValidationErrorsOccurredTitle,
            Errors = validateResults,
            Detail = null,
            Status = StatusCodes.Status400BadRequest,
            Instance = null,
        };

        context.Result = new ContentResult()
        {
            //StatusCode = Status466UserActionException,
            //StatusCode = HttpConstants.UserActionErrorCode466,
            //StatusCode = (int)HttpStatusCode.BadRequest,
            StatusCode = response.Status,
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(response)
        };
    }
}
