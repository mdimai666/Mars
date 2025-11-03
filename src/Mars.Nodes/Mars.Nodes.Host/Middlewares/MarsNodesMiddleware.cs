using System.Diagnostics;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Mappings;
using Mars.Nodes.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Middlewares;

internal class MarsNodesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LinkGenerator _linkGenerator;
    private readonly IInlineConstraintResolver _inlineConstraintResolver;
    private readonly RED _RED;
    private readonly INodeTaskManager _nodeTaskManager;

    public MarsNodesMiddleware(RequestDelegate next,
                    LinkGenerator linkGenerator,
                    RED red,
                    INodeTaskManager nodeTaskManager,
                    ILogger<MarsNodesMiddleware> logger,
                    IInlineConstraintResolver inlineConstraintResolver)
    {
        _next = next;
        _linkGenerator = linkGenerator;
        _RED = red;
        _nodeTaskManager = nodeTaskManager;
        _inlineConstraintResolver = inlineConstraintResolver;
    }

    [DebuggerStepThrough]
    public async Task InvokeAsync(HttpContext httpContext)
    {

        if (httpContext.Request.Path == "/favicon.ico") goto Next;
        if (httpContext.Request.Method == "OPTIONS") goto Next;
        if (httpContext.Request.Method == "TRACE") goto Next;
        if (httpContext.Request.Path.StartsWithSegments("/upload")) goto Next;

#if DEBUG
        //Console.WriteLine("MarsNodesMiddleware: " + httpContext.Request.Path);
#endif

        if (_RED.HttpRegisterdCatchers.Count > 0)
        {
            //context.Request.Method = "GET";
            var list = _RED.GetHttpCatchRegistersForMethod(httpContext.Request.Method);

            //запросы на ресурсы тоже ловит AppFront.styles.css appsettings.json, если разрешить Match
            //HttpCatchRegister? find = list.OrderBy(s => s.IsContainCurlyBracket) ловит ре
            //                                .FirstOrDefault(reg => reg.TryMatch(context.Request.Path, context, logger));
            HttpCatchRegister? find = list.OrderBy(s => s.IsContainCurlyBracket)
                                            .FirstOrDefault(reg => reg.Pattern == httpContext.Request.Path);

            if (find != null)
            {
                //await context.Response.WriteAsync($"middleware: {find.NodeId}");
                var ctx = new HttpInNodeHttpRequestContext(httpContext, find);
                var requestUserInfo = httpContext.RequestServices.GetRequiredService<IRequestContext>().ToRequestUserInfo();

                var msg = new NodeMsg();
                msg.Add(ctx);
                msg.Add(requestUserInfo);

                var taskId = await _nodeTaskManager.CreateJob(httpContext.RequestServices, find.NodeId, msg);

                var task = _nodeTaskManager.Get(taskId);

                if (task.IsDone && task.ErrorCount > 0 && !httpContext.Response.HasStarted)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await httpContext.Response.WriteAsync("error");
                }

                return;
            }

        }

    Next:
        await _next(httpContext);
    }
}

//public static class AA
//{

//    IInlineConstraintResolver inlineConstraintResolver;

//    public static IRouteBuilder MapVerb(
//        this IRouteBuilder builder,
//        string verb,
//        string template,
//        RequestDelegate handler)
//    {
//        var constraints = new RouteValueDictionary
//        {
//            ["httpMethod"] = new HttpMethodRouteConstraint(verb),
//        };

//        var route = new Route(
//            new RouteHandler(handler),
//            template,
//            defaults: null,
//            constraints: constraints!,
//            dataTokens: null,
//            inlineConstraintResolver: GetConstraintResolver(builder));

//        builder.Routes.Add(route);
//        return builder;
//    }

//private static IInlineConstraintResolver GetConstraintResolver(IRouteBuilder builder)
//{
//    return builder.ServiceProvider.GetRequiredService<IInlineConstraintResolver>();
//}
//}
