using Mars.Nodes.Abstractions;
using Mars.Nodes.Abstractions.HttpModule;
using Mars.Nodes.Core;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Host.Nodes;

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
        var renderEngineLocator = RNS.ServiceProvider.GetRequiredService<IWebRenderEngineLocator>();

        MarsAppFront af = renderEngineLocator.GetAppFrontForUrl("/")
            ?? throw new InvalidOperationException("Фронт для url '/' не найден. Проверьте FrontsOption (настройки фронтов).");

        http.HttpContext.Items.Add(nameof(MarsAppFront), af);

        WebPage page = WebPage.Blank(input.Payload?.ToString() ?? "");

        var render = await processor.RenderPage(page, http.HttpContext, new() { UseCache = false }, default);

        input.Payload = render.html;
        callback(input);

    }

}
