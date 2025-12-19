using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite;
using Mars.Host.Shared.WebSite.Models;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.WebApp.Nodes.Nodes;

namespace Mars.Nodes;

public class RenderPageNodeImpl : INodeImplement<RenderPageNode>, INodeImplement
{
    public RenderPageNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public RenderPageNodeImpl(RenderPageNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        HttpInNodeHttpRequestContext? http = input.Get<HttpInNodeHttpRequestContext>();

        if (http == null) throw new ArgumentNullException(nameof(http) + ":HttpInNodeHttpRequestContext");

        var processor = RED.ServiceProvider.GetRequiredService<IWebSiteProcessor>();
        var appProvider = RED.ServiceProvider.GetRequiredService<IMarsAppProvider>();

        //MarsAppFront app = appProvider.GetAppForUrl(context.Request.Path);
        MarsAppFront af = appProvider.GetAppForUrl("/");

        http.HttpContext.Items.Add(nameof(MarsAppFront), af);

        WebPage page = WebPage.Blank(input.Payload?.ToString() ?? "");

        var render = await processor.RenderPage(page, http.HttpContext, null, default);

        //var resolvedPage = processor.ResolveUrl("/url", http.HttpContext, RED.ServiceProvider);

        input.Payload = render.html;
        callback(input);

    }

}
