using System.Diagnostics;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Middlewares;

internal class MarsNodesMiddleware
{
    private readonly RequestDelegate next;
    private readonly LinkGenerator _linkGenerator;
    private readonly INodeService nodeService;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IServiceProvider serviceProvider;
    //private readonly ILogger<MarsNodesMiddleware> logger;
    private readonly IInlineConstraintResolver inlineConstraintResolver;
    private readonly RED _RED;

    //private readonly IServiceProvider serviceProvider;

    //public MarsNodesMiddleware(IServiceProvider serviceProvider)
    //{
    //    this.serviceProvider = serviceProvider;
    //}

    public MarsNodesMiddleware(RequestDelegate next,
                    LinkGenerator linkGenerator,
                    INodeService nodeService,
                    RED _RED,
                    IServiceScopeFactory scopeFactory,
                    IServiceProvider serviceProvider,
                    ILogger<MarsNodesMiddleware> logger,
                    IInlineConstraintResolver inlineConstraintResolver)
    {
        this.next = next;
        _linkGenerator = linkGenerator;
        this.nodeService = nodeService;
        this._RED = _RED;
        this.scopeFactory = scopeFactory;
        this.serviceProvider = serviceProvider;
        //this.logger = logger;
        this.inlineConstraintResolver = inlineConstraintResolver;
    }


    [DebuggerStepThrough]
    public async Task InvokeAsync(HttpContext httpContext)
    {

        if (httpContext.Request.Path == "/favicon.ico") goto Next;
        if (httpContext.Request.Method == "OPTIONS") goto Next;


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
                using var scope = serviceProvider.CreateScope();

                try
                {
                    HttpInNodeHttpRequestContext ctx = new(httpContext, find);
                    //nodeService.HttpResponseWaitList.Add(ctx);

                    NodeMsg msg = new();
                    msg.Add(ctx);
                    _ = nodeService.Inject(scope.ServiceProvider, find.NodeId, msg);//TODO: Тут кажется проблема
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    scope.Dispose();
                }

                return;
            }

        }

    Next:
        await next(httpContext);
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
