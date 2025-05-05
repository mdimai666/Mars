using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.WebSiteProcessor.Handlebars;
using Mars.WebSiteProcessor.Interfaces;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.DatabaseHandlebars;

public class DatabaseHandlebarsWebRenderEngine : HandlebarsWebRenderEngine, IWebRenderEngine
{
    MarsAppFront appFront = default!;

    //public override void AddFront(WebApplicationBuilder builder, MarsAppFront cfg)
    //{
    //    appFront = cfg;
    //}

    //public override void UseFront(WebApplication app)
    //{
    //    Initialize(appFront, app.Services);
    //}

    protected override void Initialize(MarsAppFront appFront, IServiceProvider rootServices)
    {
        this.appFront = appFront;

        var hub = rootServices.GetRequiredService<IHubContext<ChatHub>>();

        //var scope = rootServices.CreateScope();
        var eventManager = rootServices.GetRequiredService<IEventManager>();

        WebDatabaseTemplateService wts = new WebDatabaseTemplateService(rootServices, hub, appFront, eventManager);
        //WebDatabaseTemplateService wts = new WebDatabaseTemplateService(scope.ServiceProvider, hub, appFront);
        appFront.Features.Set<IWebTemplateService>(wts);

        //var wts = appFront.Features.Get<IWebTemplateService>();

        wts.OnFileUpdated += (s, e) =>
        {
            //wts.ScanSite();
            wts.ClearCache();
        };

    }

    public override string RenderPage(RenderEngineRenderRequestContext renderContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        return base.RenderPage(renderContext, serviceProvider, cancellationToken);
    }
}
