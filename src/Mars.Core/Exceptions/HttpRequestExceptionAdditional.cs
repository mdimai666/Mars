using System.Net.Http;

namespace Mars.Core.Exceptions;

public class HttpRequestExceptionAdditional : HttpRequestException
{
    public string ResponseBody { get; set; }

    public HttpRequestExceptionAdditional(HttpResponseMessage response)
    {
        ResponseBody = response.Content.ReadAsStringAsync().Result;
    }
}

public static class HttpResponseMessageExtensions
{
    public static void EnsureSuccessStatusCodeWithBody(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        //var content = await response.Content.ReadAsStringAsync();


        var ex = new HttpRequestExceptionAdditional(response);

        if (response.Content != null)
            response.Content.Dispose();

        throw ex;
        //var e = new HttpRequestExceptionAdditional(response);
        //throw new HttpRequestException("XXX", e);
    }
}
