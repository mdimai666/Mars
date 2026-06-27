using Mars.Host.Data;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Models;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.HttpModule;
using Mars.WebApp.Nodes.Nodes;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Http;

namespace Mars.Nodes;

public class MarsHostRootLayoutRenderNodeImpl : INodeImplement<MarsHostRootLayoutRenderNode>
{
    public MarsHostRootLayoutRenderNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public MarsHostRootLayoutRenderNodeImpl(MarsHostRootLayoutRenderNode node, IRuntimeNodeScope rns)
    {
        this.Node = node;
        this.RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        HttpInNodeHttpRequestContext? http = input.Get<HttpInNodeHttpRequestContext>();

        if (http == null) throw new ArgumentNullException(nameof(http) + ":HttpInNodeHttpRequestContext");

        string pageHtml = input.Payload?.ToString() ?? "";

        ////var app = await Microsoft.AspNetCore.Html.RenderComponentAsync<App>(RenderMode.Static);
        //IHtmlHelper htmlHelper = RNS.ServiceProvider.GetRequiredService<IHtmlHelper>();
        ////Microsoft.AspNetCore.Html.IHtmlContent html = await htmlHelper.RenderComponentAsync<App>(RenderMode.Static);
        //Microsoft.AspNetCore.Html.IHtmlContent html = await htmlHelper.RenderComponentAsync<>(RenderMode.Static);

        //IViewEngine viewEngine = http.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
        //ViewEngineResult viewResult = viewEngine.FindView(http.HttpContext.Response, nameof(Pages__Host), true);

        //using var ef = RNS.GetService<MarsDbContextLegacy>();

        var rq = RNS.ServiceProvider.GetService<IRequestContext>();
        var httpContext = http.HttpContext;
        var optionsService = RNS.ServiceProvider.GetRequiredService<IOptionService>();
        var userService = RNS.ServiceProvider.GetRequiredService<IUserService>();

        var af = (httpContext.Items[nameof(MarsAppFront)] as MarsAppFront)!;
        var request = new WebClientRequest(httpContext.Request);
        var userDetail = rq.IsAuthenticated ? await userService.GetDetail(rq.User.Id, default) : null;

        throw new NotImplementedException();
        //var html = new PrepareHostHtml(af, optionsService, request, userDetail, new RenderParam(), RNS.ServiceProvider, default);

        //string rendered = html.BeforeBodyHtml + "\n" + pageHtml + "\n" + html.AfterBodyHtml;

        //input.Payload = rendered;
        //callback(input);

    }

}
