using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite;
using Mars.Host.Shared.WebSite.Models;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.HttpModule;
using Mars.WebApp.Nodes.Nodes;

namespace Mars.Nodes;

public class RenderPageNodeImpl : INodeImplement<RenderPageNode>
{
    public RenderPageNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public RenderPageNodeImpl(RenderPageNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        HttpInNodeHttpRequestContext? http = input.Get<HttpInNodeHttpRequestContext>();

        if (http == null) throw new ArgumentNullException(nameof(http) + ":HttpInNodeHttpRequestContext");

        var processor = RNS.ServiceProvider.GetRequiredService<IWebSiteProcessor>();
        var appProvider = RNS.ServiceProvider.GetRequiredService<IMarsAppProvider>();

        //MarsAppFront app = appProvider.GetAppForUrl(context.Request.Path);
        MarsAppFront af = appProvider.GetAppForUrl("/");

        http.HttpContext.Items.Add(nameof(MarsAppFront), af);

        WebPage page = WebPage.Blank(input.Payload?.ToString() ?? "");

        var render = await processor.RenderPage(page, http.HttpContext, new() { UseCache = false }, default);

        //var resolvedPage = processor.ResolveUrl("/url", http.HttpContext, RNS.ServiceProvider);

        input.Payload = render.html;
        callback(input);

    }

}
