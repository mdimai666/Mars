using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HttpResponseNode/HttpResponseNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpResponseNode : Node
{
    public override string DisplayName => base.Label + $":{ResponseStatusCode}";

    public int ResponseStatusCode { get; set; } = 200;
    public HeaderItem[] Headers { get; set; } = [];

    public HttpResponseNode()
    {
        isInjectable = false;
        Color = "#e7e6af";
        Inputs = [new()];
        //Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/web-48.png";
    }

    public static IReadOnlyDictionary<int, string> StatusCodes = new Dictionary<int, string>()
    {
        [200] = "200 OK",
        [201] = "201 Created",
        [202] = "202 Accepted",
        [204] = "204 NoContent",
        [301] = "301 MovedPermanently",
        [302] = "302 Found",
        [304] = "304 NotModified",
        [307] = "307 TemporaryRedirect",
        [308] = "308 PermanentRedirect",
        [400] = "400 BadRequest",
        [401] = "401 Unauthorized",
        [402] = "402 PaymentRequired",
        [403] = "403 Forbidden",
        [404] = "404 NotFound",
        [405] = "405 MethodNotAllowed",
        [406] = "406 NotAcceptable",
        [409] = "409 Conflict",
        [413] = "413 RequestEntityTooLarge", // RFC 2616, renamed
        [413] = "413 PayloadTooLarge", // RFC 7231
        [418] = "418 ImATeapot",
        [500] = "500 InternalServerError",
        [501] = "501 NotImplemented",
        [502] = "502 BadGateway",
        [503] = "503 ServiceUnavailable",
    };

    public static IReadOnlyCollection<string> ExampleResponseHeaders =
    [
        // Content types
        "Content-Type=application/json; charset=utf-8",
        "Content-Type=application/xml",
        "Content-Type=text/html; charset=utf-8",
        "Content-Type=text/html; charset=windows-1251",
        "Content-Type=text/plain",
        "Content-Type=application/octet-stream",

        // Caching
        "Cache-Control=no-store, no-cache, must-revalidate",
        "Cache-Control=public, max-age=31536000",
        "Pragma=no-cache",
        "Expires=0",
        "ETag=\"33a64df551425fcc55e4d42a148795d9f25f89d4\"",
        "Last-Modified=Wed, 21 Oct 2015 07:28:00 GMT",

        // Security
        "Content-Security-Policy=default-src 'self'",
        "X-Content-Type-Options=nosniff",
        "X-Frame-Options=DENY",
        //"Strict-Transport-Security=max-age=63072000; includeSubDomains; preload",
        "X-XSS-Protection=0",

        // CORS (Cross-Origin Resource Sharing)
        "Access-Control-Allow-Origin=*",
        "Access-Control-Allow-Origin=https://example.com",
        "Access-Control-Allow-Methods=GET, POST, PUT, DELETE, OPTIONS",
        "Access-Control-Allow-Headers=Content-Type, Authorization, X-Requested-With",
        "Access-Control-Allow-Credentials=true",
        "Access-Control-Expose-Headers=X-Custom-Header",

        // Server & Session info
        "Server=nginx",
        "Server=Kestrel",
        "Set-Cookie=session_id=xyz123; Secure; HttpOnly; SameSite=Strict",
        "Date=Wed, 28 May 2026 12:00:00 GMT",

        // Connection & Payload info
        "Connection=keep-alive",
        "Content-Encoding=gzip",
        "Content-Encoding=br",
        "Content-Length=1024",

        // Redirects & Auth challenges
        "Location=https://example.com",
        "WWW-Authenticate=Bearer error=\"invalid_token\"",

        // Custom API example
        "X-Response-Time=42ms"
    ];
}
