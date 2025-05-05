using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Mars.Core.Constants;
using Mars.Core.Extensions;
using Mars.Shared.Common;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Integration.Tests.Extensions;

public static class FlurlExtensions
{
    private static JsonSerializerOptions _options = new JsonSerializerOptions() { WriteIndented = true };

    [DebuggerStepThrough]
    public static async Task<IFlurlResponse> CatchUserActionError(this Task<IFlurlResponse> flurlResponse)
    {
        try
        {
            var response = await flurlResponse.ConfigureAwait(false);
            return response;
        }
        catch (FlurlHttpException ex) when (ex.StatusCode is HttpConstants.UserActionErrorCode466 or StatusCodes.Status500InternalServerError)
        {
            var data = await ex.GetResponseJsonAsync<UserActionResult>().ConfigureAwait(false);
            var prefix = ex.StatusCode + ": ";
            var detail = JsonSerializer.Serialize(data.DetailMessages, _options)?.TextEllipsis(300);
            Assert.Fail(prefix + data.Message + "\n" + detail);
            throw;
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == StatusCodes.Status400BadRequest)
        {
            var prefix = "400: ";
            var validationError = await ex.GetResponseJsonAsync<ValidationProblemDetails>();
            var detail = JsonSerializer.Serialize(validationError.Errors, _options);
            Assert.Fail(prefix + validationError.Title + "\n" + detail);
            throw;
        }
    }

    [DebuggerStepThrough]
    public static async Task<ValidationProblemDetails> ReceiveValidationError(this Task<IFlurlResponse> flurlResponse)
    {
        try
        {
            using var response = await flurlResponse.ConfigureAwait(false);
            var messsage = $"Response should be fail, but it is successful. Status = {response.StatusCode}";
            Assert.Fail(messsage);
            throw new Exception(messsage);
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == StatusCodes.Status400BadRequest)
        {
            var validationError = await ex.GetResponseJsonAsync<ValidationProblemDetails>();
            //var detail = JsonSerializer.Serialize(validationError.Errors, _options);
            //Assert.Fail(prefix + validationError.Title + "\n" + detail);
            //throw;
            return validationError;
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == HttpConstants.UserActionErrorCode466)
        {
            var data = await ex.GetResponseJsonAsync<UserActionResult>().ConfigureAwait(false);
            var prefix = HttpConstants.UserActionErrorCode466 + ": ";
            var detail = JsonSerializer.Serialize(data.DetailMessages, _options)?.TextEllipsis(300);
            Assert.Fail(prefix + data.Message + "\n" + detail);
            throw;
        }
    }

    public static int AsInt(this HttpStatusCode statusCode) => (int)statusCode;
}
