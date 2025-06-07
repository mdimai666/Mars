using Flurl.Http;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.WebApiClient.Models;

namespace Mars.WebApiClient.Implements;

internal class BasicServiceClient
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly IFlurlClient _client;
    protected string _basePath;
    protected string _controllerName = default!;

    public BasicServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient)
    {
        _serviceProvider = serviceProvider;
        _basePath = "/api/";
        _client = flurlClient;
    }

    public static Action<FlurlCall> OnError = call =>
    {
        if (call.Response == null && !call.Completed)
        {
            call.ExceptionHandled = true;
            throw new HttpRequestException("ServerNotRespondingException");
        }

        if (call.Response.StatusCode == (int)System.Net.HttpStatusCode.BadRequest)
        {
            var problemDetails = call.Response.GetJsonAsync<AspNetValidationProblemDetails>().ConfigureAwait(false).GetAwaiter().GetResult();

            call.ExceptionHandled = true;

            throw new MarsValidationException(problemDetails?.Title ?? ExtractResponseErrorMessage(call), problemDetails.Errors, call.Exception);
        }
        else if (call.Response.StatusCode == HttpConstants.UserActionErrorCode466)
        {
            var detail = call.Response.GetJsonAsync<UserActionResult>().ConfigureAwait(false).GetAwaiter().GetResult();

            call.ExceptionHandled = true;

            throw new UserActionException(detail?.Message ?? ExtractResponseErrorMessage(call), detail?.DetailMessages, call.Exception);
        }
        else if (call.Response.StatusCode == HttpConstants.ForbiddenCode403)
        {
            var detail = call.Response.GetJsonAsync<UserActionResult>().ConfigureAwait(false).GetAwaiter().GetResult();

            call.ExceptionHandled = true;

            throw new ForbiddenException(detail?.Message ?? ExtractResponseErrorMessage(call), call.Exception);
        }
        else if (call.Response.StatusCode == (int)System.Net.HttpStatusCode.Unauthorized)
        {
            var detail = call.Response.GetJsonAsync<UserActionResult>().ConfigureAwait(false).GetAwaiter().GetResult();

            call.ExceptionHandled = true;

            throw new UnauthorizedException(detail?.Message ?? ExtractResponseErrorMessage(call), call.Exception);
        }
    };

    private static string ExtractResponseErrorMessage(FlurlCall call)
        => call.Response.ResponseMessage.ReasonPhrase ?? call.Exception?.Message ?? call.Response.StatusCode.ToString();
    private static string ExtractResponseErrorMessage(IFlurlResponse response)
        => response.ResponseMessage.ReasonPhrase ?? response.StatusCode.ToString();

    protected static Action<FlurlCall> OnStatus404ReturnNull = call =>
    {
        if (call.Response.StatusCode == (int)System.Net.HttpStatusCode.NotFound)
        {
            call.ExceptionHandled = true;
        }
    };

    protected static Action<FlurlCall> OnStatus404ThrowException = call =>
    {
        if (call.Response.StatusCode == (int)System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException(innerException: call.Exception);
        }
    };

    protected void HandleResponseGeneralErrors(IFlurlResponse res)
    {
        if (res.StatusCode == (int)System.Net.HttpStatusCode.BadRequest)
        {
            var problemDetails = res.GetJsonAsync<AspNetValidationProblemDetails>().ConfigureAwait(false).GetAwaiter().GetResult();
            throw new MarsValidationException(problemDetails?.Title ?? ExtractResponseErrorMessage(res), problemDetails.Errors);
        }
        else if (res.StatusCode == HttpConstants.UserActionErrorCode466)
        {
            var detail = res.GetJsonAsync<UserActionResult>().ConfigureAwait(false).GetAwaiter().GetResult();
            throw new UserActionException(detail?.Message ?? ExtractResponseErrorMessage(res), detail?.DetailMessages);
        }
        else if (res.StatusCode == HttpConstants.ForbiddenCode403)
        {
            var detail = res.GetJsonAsync<UserActionResult>().ConfigureAwait(false).GetAwaiter().GetResult();
            throw new ForbiddenException(detail?.Message ?? ExtractResponseErrorMessage(res));
        }
        else if (res.StatusCode == (int)System.Net.HttpStatusCode.Unauthorized)
        {
            var detail = res.GetJsonAsync<UserActionResult>().ConfigureAwait(false).GetAwaiter().GetResult();
            throw new UnauthorizedException(detail?.Message ?? ExtractResponseErrorMessage(res));
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}

/// <summary>
/// <see href="https://flurl.dev/docs/configuration/#event-handlers"/>
/// </summary>
internal class MarsWebApiClientErrorEventHandler : FlurlEventHandler
{
    public override void Handle(FlurlEventType eventType, FlurlCall call)
    {
        if (eventType == FlurlEventType.OnError)
        {

        }

        base.Handle(eventType, call);
    }
}
