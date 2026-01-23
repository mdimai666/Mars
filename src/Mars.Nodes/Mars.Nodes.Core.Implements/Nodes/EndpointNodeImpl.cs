using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HandlebarsDotNet;
using Mars.Host.Shared.Extensions;
using Mars.Host.Shared.Interfaces;
using Mars.Nodes.Core.Implements.Extensions;
using Mars.Nodes.Core.Implements.Utils;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared.HttpModule;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class EndpointNodeImpl : INodeImplement<EndpointNode>, INodeImplement
{
    public EndpointNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public EndpointNodeImpl(EndpointNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;

        var mw = new HttpCatchRegister(Node.Method, node.UrlPattern, node.Id);

        _RED.RegisterHttpMiddleware(mw);
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var http = input.Get<HttpInNodeHttpRequestContext>();

        var problem = ValidateRequestRequirements();
        if (problem is not null)
        {
            http.HttpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status400BadRequest;
            await http.HttpContext.Response.WriteAsJsonAsync(problem);
            return;
        }

        if (Node.EndpointInputModel == EndpointInputModelType.String)
        {
            string body = await http.HttpContext.GetRequestBodyAsStringAsync();
            input.Payload = body;
            callback(input);
        }
        else if (Node.EndpointInputModel == EndpointInputModelType.JsonSchema)
        {
            var jsonBody = await http.HttpContext.TryReadJsonAsync(parameters.CancellationToken);

            if (jsonBody is null)
            {
                await WriteValidationErrorsResponseAsync(["RequestBodyIsNotValidJson"], http.HttpContext);
                return;
            }

            try
            {
                var result = EndpointJsonSchemaTool.ValidateAndFilter(schemaJson: Node.JsonSchema, jsonBody);

                if (!result.IsValid)
                {
                    var detail = $"The JSON payload does not conform to the required schema. (NodeId='{Node.Id}')";
                    await WriteValidationErrorsResponseAsync(result.Errors.ToArray(), http.HttpContext, detail: detail);
                    return;
                }

                input.Payload = result.ValidatedJson;
                callback(input);
            }
            catch (JsonException ex)
            {
                await WriteValidationErrorsResponseAsync([ex.Message], http.HttpContext);
                return;
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    #region TOOLS
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

    Task WriteValidationErrorsResponseAsync(IDictionary<string, string[]> errors,
                    HttpContext httpContext,
                    int statusCode = StatusCodes.Status400BadRequest,
                    string? title = null,
                    string? detail = null)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = new ValidationProblemDetails()
        {
            Title = title ?? AppRes.ValidationErrorsOccurredTitle,
            Errors = errors,
            Detail = detail,
            Status = statusCode,
            Instance = null,
        };

        return httpContext.Response.WriteAsJsonAsync(response);
    }

    Task WriteValidationErrorsResponseAsync(string[] errors,
                    HttpContext httpContext,
                    int statusCode = StatusCodes.Status400BadRequest,
                    string? title = null,
                    string? detail = null)
    {
        return WriteValidationErrorsResponseAsync(
            new Dictionary<string, string[]>
            {
                ["$"] = errors
            },
            httpContext,
            statusCode,
            title: title,
            detail: detail);
    }

    Task WriteValidationErrorsResponseAsync(IReadOnlyCollection<ValidationResult> errors,
                    HttpContext httpContext,
                    int statusCode = StatusCodes.Status400BadRequest,
                    string? title = null,
                    string? detail = null)
    {
        return WriteValidationErrorsResponseAsync(
            errors.ToProblemDetailsErrors(),
            httpContext,
            statusCode,
            title: title,
            detail: detail);
    }
    #endregion

}
