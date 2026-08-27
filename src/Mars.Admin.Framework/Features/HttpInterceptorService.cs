using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Toolbelt.Blazor;

namespace AppFront.Shared.Features;

public class HttpInterceptorService
{
    private readonly HttpClientInterceptor _interceptor;
    private readonly NavigationManager _navManager;
    private readonly ILogger _logger;

    public HttpInterceptorService(HttpClientInterceptor interceptor, NavigationManager navManager, ILogger logger)
    {
        _interceptor = interceptor;
        _navManager = navManager;
        _logger = logger;
    }
    public void RegisterEvent() => _interceptor.AfterSend += InterceptResponse;
    private void InterceptResponse(object? sender, HttpClientInterceptorEventArgs e)
    {
        Console.WriteLine("InterceptResponse");

        string message = string.Empty;
        if (!e.Response.IsSuccessStatusCode)
        {
            var statusCode = e.Response.StatusCode;
            switch (statusCode)
            {
                case HttpStatusCode.NotFound:
                    _navManager.NavigateTo("/404");
                    message = "The requested resorce was not found.";
                    _logger.LogError(message);
                    break;
                case HttpStatusCode.Unauthorized:
                    _navManager.NavigateTo("/unauthorized");
                    message = "User is not authorized";
                    _logger.LogError(message);
                    break;
                default:
                    _navManager.NavigateTo("/500");
                    message = "Something went wrong, please contact Administrator";
                    _logger.LogError(message);
                    break;
            }
            throw new HttpResponseException(message);
        }
    }
    public void DisposeEvent() => _interceptor.AfterSend -= InterceptResponse;
}

[Serializable]
internal class HttpResponseException : Exception
{
    public HttpResponseException()
    {
    }
    public HttpResponseException(string message)
        : base(message)
    {
    }
    public HttpResponseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

}
