using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Mars.Host.Exceptions
{
    internal class Mlock
    {

    }
    //public class HttpRequestExceptionAdditional : HttpRequestException
    //{
    //    public HttpResponseMessage Response { get; set; }

    //    public HttpRequestExceptionAdditional(HttpResponseMessage response)
    //    {
    //        Response = response;
    //    }
    //}

    //public class HttpResponseMessage2 : IDisposable
    //{
    //    public HttpResponseHeaders Headers { get; }
    //    public bool IsSuccessStatusCode { get; }
    //    public string ReasonPhrase { get; set; }
    //    public HttpRequestMessage RequestMessage { get; set; }
    //    public HttpStatusCode StatusCode { get; set; }
    //    public Version Version { get; set; }
    //    public HttpContent Content { get; set; }

    //    public HttpResponseMessage2(HttpResponseMessage res)
    //    {
    //        Headers = res.Headers;
    //        IsSuccessStatusCode = res.IsSuccessStatusCode;
    //        ReasonPhrase = res.ReasonPhrase;
    //        RequestMessage = res.RequestMessage;
    //        StatusCode = res.StatusCode;
    //        Version = res.Version;
    //        Content = res.Content;

    //    }

    //    public void Dispose()
    //    {

    //    }
    //}

    //public static class HttpResponseMessageExtensions
    //{
    //    public static void EnsureSuccessStatusCodeAdditional(this HttpResponseMessage response)
    //    {
    //        if (response.IsSuccessStatusCode)
    //        {
    //            return;
    //        }

    //        //var content = await response.Content.ReadAsStringAsync();

    //        if (response.Content != null)
    //            response.Content.Dispose();

    //        throw new HttpRequestExceptionAdditional(response);
    //    }
    //}
}
