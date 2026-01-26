using System.Diagnostics;
using Mars.Host.Shared.Interfaces;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Mappings;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Host.Shared.HttpModule;
using Mars.Nodes.Host.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Middlewares;

internal class MarsNodesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RED _RED;
    private readonly INodeTaskManager _nodeTaskManager;

    public MarsNodesMiddleware(RequestDelegate next,
                    RED red,
                    INodeTaskManager nodeTaskManager)
    {
        _next = next;
        _RED = red;
        _nodeTaskManager = nodeTaskManager;
    }

    [DebuggerStepThrough]
    public async Task InvokeAsync(HttpContext httpContext)
    {
        // app.UseStaticFiles должно быть раньше этого
        if (httpContext.Request.Path == "/favicon.ico") goto Next;
        if (httpContext.Request.Method == "OPTIONS") goto Next;
        if (httpContext.Request.Method == "TRACE") goto Next;
        if (httpContext.Request.Path.StartsWithSegments("/upload")) goto Next;
        if (httpContext.Request.Path.StartsWithSegments("/api")) goto Next;

#if DEBUG
        //Console.WriteLine("MarsNodesMiddleware: " + httpContext.Request.Path);
#endif

        if (_RED.HttpRegisterdCatchers.Count > 0)
        {
            var foundRoute = _RED.CompiledHttpRouteMatcher.Match(httpContext.Request.Path, out var routeValues);

            if (foundRoute is not null)
            {
                try
                {
                    using var ctx = new HttpInNodeHttpRequestContext(httpContext, foundRoute, routeValues);
                    var requestUserInfo = httpContext.RequestServices.GetRequiredService<IRequestContext>().ToRequestUserInfo();

                    var msg = new NodeMsg();
                    msg.Add(ctx);
                    msg.Add(requestUserInfo);

                    using var scope = _RED.ServiceProvider.CreateScope();

                    // Тут надо подумать. RED context у нод свой и он по недодоуманности удерживается.
                    //var taskId = await _nodeTaskManager.CreateJob(httpContext.RequestServices, foundRoute.NodeId, msg);
                    var taskId = await _nodeTaskManager.CreateJob(scope.ServiceProvider, foundRoute.NodeId, msg);

                    var task = _nodeTaskManager.Get(taskId);

                    if (task.IsDone && task.ErrorCount > 0 && !httpContext.Response.HasStarted)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await httpContext.Response.WriteAsync("error");
                    }
                }
                finally
                {
                    // Возвращаем в пул после использования!
                    if (routeValues is not null)
                        CompiledHttpRouteMatcher.RouteValuePools.Return(routeValues);
                }
                return;
            }

        }

    Next:
        await _next(httpContext);
    }
}
