using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Models;
using Mars.Shared.Contracts.WebSite.Models;
using Mars.Shared.Options;
using Mars.WebSiteProcessor.Interfaces;

namespace Mars.WebSiteProcessor.Services;

public class PrepareHostHtml
{
    public string BeforeBodyHtml { get; }
    public string AfterBodyHtml { get; }

    public PrepareHostHtml(
        MarsAppFront appFront,
        IOptionService optionService,
        WebClientRequest webClientRequest,
        UserDetail? userDetail,
        RenderParam renderParam,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        FrontOptions frontOpt = optionService.GetOption<FrontOptions>();
        var hostHtml = frontOpt.HostItems.FirstOrDefault(s=>s.Url == appFront.Configuration.Url)?.HostHtml!;

        if (string.IsNullOrEmpty(hostHtml))
        {
            BeforeBodyHtml = "";
            AfterBodyHtml = "";
        }

        string cacheKey = GetRenderKey(appFront);

        //var renderEngine = new HandlebarsWebRenderEngine();
        var renderEngine = appFront.Features.Get<IWebRenderEngine>();
        //var context = new RenderEngineRenderRequestContext2(webClientRequest, appFront,  ;

        var pageRenderContext = new PageRenderContext()
        {
            Request = webClientRequest,
            SysOptions = optionService.SysOption,
            User = userDetail == null ? null : new RenderContextUser(userDetail),
            RenderParam = renderParam,
            IsDevelopment = optionService.IsDevelopment,
        };

        var webRoot = new WebRoot(new WebSitePart(WebSitePartType.Root, "PrepareHostHtml", "PrepareHostHtml.cs", "", hostHtml, new Dictionary<string, string>(), "PrepareHostHtml"));

        var html = renderEngine.RenderPage(appFront, pageRenderContext, webRoot, null, null, serviceProvider, cancellationToken);

        var splitter = new RootPageBodyTagSplitter(html);

        BeforeBodyHtml = splitter.PreBody;
        AfterBodyHtml = splitter.AfterBody;
    }

    static string GetRenderKey(MarsAppFront appFront)
    {
        string key = $"{appFront.Configuration.Url}::HostHtml";
        return key;
    }
}
