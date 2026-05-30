using Mars.Core.Extensions;
using Mars.Host.Shared.Interfaces;
using Mars.Nodes.Core.Implements.Extensions;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared.HttpModule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class HttpInNodeImpl : INodeImplement<HttpInNode>, INodeImplement
{
    public HttpInNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public HttpInNodeImpl(HttpInNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;

        var mw = new HttpCatchRegister(Node.Method, node.UrlPattern, node.Id);

        _RED.RegisterHttpMiddleware(mw);
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var http = input.Get<HttpInNodeHttpRequestContext>();
        var request = http.HttpContext.Request;

        var problem = ValidateRequestRequirements();
        if (problem is not null)
        {
            http.HttpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status400BadRequest;
            await http.HttpContext.Response.WriteAsJsonAsync(problem);
            return;
        }

        var isContentTypeMultipart = request.ContentType?.StartsWith("multipart/form-data") == true;

        if (isContentTypeMultipart && !Node.AllowMultipart)
        {
            http.HttpContext.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            await http.HttpContext.Response.WriteAsync("Multipart not allowed");
            return;
        }

        if (isContentTypeMultipart)
        {
            long? contentLength = request.ContentLength;
            long maxLimitInBytes = DataSizeParser.ParseToBytes(Node.MaxFileSize.AsNullIfEmpty() ?? "10mb");

            if (contentLength.HasValue && contentLength.Value > maxLimitInBytes)
            {
                http.HttpContext.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await http.HttpContext.Response.WriteAsync("File size exceeds limit");
                return;
            }

            var form = await request.ReadFormAsync(cancellationToken: parameters.CancellationToken);

            // Дополнительная точечная проверка каждого файла (на случай, если Content-Length не был указан)
            if (form.Files.Any(f => f.Length > maxLimitInBytes))
            {
                http.HttpContext.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await http.HttpContext.Response.WriteAsync("File size exceeds limit");
                return;
            }

            input.Payload = form;
        }
        else if (request.HasFormContentType)
        {
            var form = await http.HttpContext.Request.ReadFormAsync(cancellationToken: parameters.CancellationToken);
            input.Payload = form;
        }
        else if (request.HasJsonContentType())
        {
            var jsonBody = await http.HttpContext.TryReadJsonAsync(parameters.CancellationToken);
            input.Payload = jsonBody;
        }
        else
        {
            string body = await http.HttpContext.GetRequestBodyAsStringAsync(cancellationToken: parameters.CancellationToken);
            input.Payload = body;
        }

        callback(input);
    }

    private ProblemDetails? ValidateRequestRequirements()
    {
        if (Node.IsRequireAuthorize)
        {
            var requestContext = RED.ServiceProvider.GetRequiredService<IRequestContext>();

            if (!requestContext.IsAuthenticated || requestContext.User is null)
            {
                return new ProblemDetails
                {
                    Type = "https://datatracker.ietf.org/doc/html/rfc9110#name-401-unauthorized",
                    Title = "401 Unauthorized",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = null,
                    //Instance = httpContext.Request.Path
                };
            }

            if (Node.AllowedRoles.Any())
            {
                var hasRole = requestContext.Roles?.Any(r => Node.AllowedRoles.Contains(r)) ?? false;
                if (!hasRole)
                {
                    return new ProblemDetails
                    {
                        Type = "https://datatracker.ietf.org/doc/html/rfc9110#name-403-forbidden",
                        Title = "403 Forbidden",
                        Status = StatusCodes.Status403Forbidden,
                        Detail = null,
                        //Instance = httpContext.Request.Path
                    };
                }
            }
        }
        return null;
    }
}
