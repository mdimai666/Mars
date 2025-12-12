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
    private readonly RED _RED;
    private readonly INodeTaskManager _nodeTaskManager;
    private readonly ILogger<MarsNodesMiddleware> _logger;

    public MarsNodesMiddleware(RequestDelegate next,
                    RED red,
                    INodeTaskManager nodeTaskManager,
                    ILogger<MarsNodesMiddleware> logger)
    {
        _next = next;
        _RED = red;
        _nodeTaskManager = nodeTaskManager;
        _logger = logger;
    }

    [DebuggerStepThrough]
    public async Task InvokeAsync(HttpContext httpContext)
    {

        if (httpContext.Request.Path == "/favicon.ico") goto Next;
        if (httpContext.Request.Method == "OPTIONS") goto Next;
        if (httpContext.Request.Method == "TRACE") goto Next;
        if (httpContext.Request.Path.StartsWithSegments("/upload")) goto Next;
        if (httpContext.Request.Path.StartsWithSegments("/api")) goto Next;

#if DEBUG
        //Console.WriteLine("MarsNodesMiddleware: " + httpContext.Request.Path);
        _logger.LogInformation(httpContext.Request.Path);
#endif

        if (_RED.HttpRegisterdCatchers.Count > 0)
        {
            var list = _RED.GetHttpCatchRegistersForMethod(httpContext.Request.Method);
            RouteValueDictionary? routeValues = null;

            //запросы на ресурсы тоже ловит AppFront.styles.css appsettings.json, если разрешить Match
            HttpCatchRegister? find = list.OrderBy(s => s.IsContainCurlyBracket)
                                            .FirstOrDefault(reg => reg.TryMatch(httpContext.Request.Path, out routeValues));

            if (find is not null)
            {
                //await context.Response.WriteAsync($"middleware: {find.NodeId}");
                var ctx = new HttpInNodeHttpRequestContext(httpContext, find, routeValues);
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
