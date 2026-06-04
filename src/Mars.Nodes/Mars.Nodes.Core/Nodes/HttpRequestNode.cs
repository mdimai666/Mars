using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Core.Extensions;
using Mars.Nodes.Core.Fields;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HttpRequestNode/HttpRequestNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpRequestNode : Node
{
    public override string DisplayName => Name.AsNullIfEmpty() ?? Url.AsNullIfEmpty() ?? base.Label;
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = "http://localhost";

    public static readonly string[] MethodVariants = ["GET", "POST", "PUT", "DELETE", "HEAD", "PATCH", "PATCH"];

    public ReturnResponseType ReturnResponse { get; set; } = ReturnResponseType.Auto;

    public HeaderItem[] Headers { get; set; } = [];

    public InputConfig<AuthFlowConfigNode> AuthConfig { get; set; }

    public HttpRequestNode()
    {
        Inputs = [new()];
        Color = "#e7e6af";
        Outputs = [new NodeOutput()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/web2-48.png";
    }

    public enum ReturnResponseType
    {
        Auto,
        String,
        Object,
        Bytes,
        Stream
    }

    public static IReadOnlyCollection<string> ExampleHeaders =
    [
        // Content Types & Negotiation
        "Content-Type=application/json; charset=utf-8",
        "Content-Type=application/x-www-form-urlencoded",
        "Content-Type=text/plain",
        "Content-Type=application/xml",
        "Content-Type=text/html",
        "Content-Type=multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW",

        // Accept Headers
        "Accept=application/json",
        "Accept=application/xml",
        "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
        "Accept-Encoding=gzip, deflate, br, zstd",
        "Accept-Language=en-US,en;q=0.9",
        "Accept-Language=ru-RU,ru;q=0.8,en-US;q=0.5",

        // Authorization & Credentials
        "Authorization=Bearer <token>",
        "Authorization=Basic dXNlcjpwYXNzd29yZA==", // user:password в Base64
        "Authorization=ApiKey <key>",
        "Cookie=session_id=abc123xyz; theme=dark",

        // Cache Control
        "Cache-Control=no-cache",
        "Cache-Control=no-store",
        "Cache-Control=max-age=0",
        "Pragma=no-cache",

        // Conditional Requests (Условные запросы)
        "If-None-Match=\"33a64df551425fcc55e4d42a148795d9f25f89d4\"",
        "If-Modified-Since=Wed, 21 Oct 2015 07:28:00 GMT",

        // Origin & Referer (CORS / Безопасность)
        "Origin=https://my-app.com",
        "Referer=https://my-app.com",
        "Host=://example.com",

        // CORS Preflight (Предварительные запросы)
        "Access-Control-Request-Method=POST",
        "Access-Control-Request-Headers=X-Requested-With, Content-Type, Authorization",

        // Network & Proxy (Прокси и инфраструктура)
        "Connection=keep-alive",
        "Connection=close",
        "X-Forwarded-For=192.168.0.1", // Реальный IP клиента за прокси
        "X-Forwarded-Proto=https",      // Исходный протокол (http/https)
        "X-Real-IP=192.168.0.1",

        // Client Info & Modern Browsers (User-Agent и Client Hints)
        "User-Agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "User-Agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
        "Sec-Ch-Ua=\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"",
        "Sec-Ch-Ua-Mobile=?0",
        "Sec-Ch-Ua-Platform=\"Windows\"",

        // Security & Privacy Context
        "Sec-Fetch-Site=same-site",
        "Sec-Fetch-Mode=cors",
        "Sec-Fetch-Dest=empty",
        "DNT=1", // Do Not Track

        // WebSockets Upgrade
        "Upgrade=websocket",
        "Sec-WebSocket-Key=dGhlIHNhbXBsZSBub25jZQ==",
        "Sec-WebSocket-Version=13",

        // Custom API Examples
        "X-Api-Key=<your-api-key>",
        "X-Requested-With=XMLHttpRequest", // Сигнализирует об AJAX-запросе
        "X-Correlation-ID=<guid>", // Для сквозного логирования
        "X-Client-Version=1.0.2"
    ];
}

public record HeaderItem
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
