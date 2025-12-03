using Mars.Core.Models;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.WebSiteProcessor.DatabaseHandlebars;
using Mars.WebSiteProcessor.Interfaces;
using Mars.WebSiteProcessor.Services;
using Mars.WebSiteProcessor.WebSite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Mars.WebSiteProcessor.Blazor;

public class BlazorWebRenderEngine : DatabaseHandlebarsWebRenderEngine, IWebRenderEngine
{
    public override void AddFront(WebApplicationBuilder builder, MarsAppFront appFront)
    {
        AppFront = appFront;
        var appFrontConfig = appFront.Configuration;
        var mode = appFrontConfig.Mode;

        ArgumentException.ThrowIfNullOrEmpty(appFront.Configuration.Path, "configured theme path is null or empty");

        if (!Directory.Exists(appFront.Configuration.Path))
            throw new DirectoryNotFoundException($"configured theme dir not exist '{appFront.Configuration.Path}'");

        if (mode == AppFrontMode.ServeStaticBlazor)
        {

        }
        else if (mode == AppFrontMode.BlazorPrerender)
        {
            var appDll = DetermineAppRuntimeDllName(appFront.Configuration.Path);
            var dllPath = Path.Combine(appFront.Configuration.Path, appDll);
            var asm = builder.AddBlazorWebAssemblyRuntime(dllPath);

            var rootNamespace = asm.GetName().Name!;

            var appType = asm.GetType(rootNamespace + ".App") ?? throw new ArgumentNullException($"on tempalte '{dllPath}' not found 'App.cs' type");

            //Set IsPrerender
            //FieldInfo? piShared = appType.GetField("IsPrerender");
            //piShared?.SetValue(null, true);

            appFront.Features.Set<AppFrontBlazorAsm>(new() { AppType = appType, FilesPath = appFront.Configuration.Path });

            //AppFrontBlazorInstance.AppType = appType;
            //AppSharedSettings.Program = programType;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public override void UseFront(IApplicationBuilder app)
    {
        Initialize(AppFront, app.ApplicationServices);

        var front = app;
        var appFrontConfig = AppFront.Configuration;
        var APP_wwwroot = Path.Combine(appFrontConfig.Path, "wwwroot");

        if (Directory.Exists(APP_wwwroot))
        {
            front.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(APP_wwwroot),
                //RequestPath = new PathString("/app"),
                ServeUnknownFileTypes = true
            });
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapFallbackToPage("/_Host");
        });
    }

    protected override void Initialize(MarsAppFront appFront, IServiceProvider rootServices)
    {
        var hub = rootServices.GetRequiredService<IHubContext<ChatHub>>();

        //var scope = rootServices.CreateScope();
        var eventManager = rootServices.GetRequiredService<IEventManager>();

        //WebDatabaseTemplateService wts = new WebDatabaseTemplateService(rootServices, hub, appFront);
        var wts = new WebDatabaseTemplateService(rootServices, hub, appFront, eventManager);
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

    public async Task<string> RenderBlazorPageWithHbsRoot(MarsAppFront appFront, string blazorPageHtml, HttpContext httpContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var webClientRequest = new WebClientRequest(httpContext.Request);
        var optionService = serviceProvider.GetRequiredService<IOptionService>();
        var requestContext = serviceProvider.GetRequiredService<IRequestContext>();
        var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
        var userDetail = requestContext.IsAuthenticated ? await userRepository.GetAuthorizedUserInformation(requestContext.User.Id, cancellationToken) : null;
        var renderParam = new RenderParam();
        var pageRenderContext = new PageRenderContext()
        {
            Request = webClientRequest,
            SysOptions = optionService.SysOption,
            User = userDetail == null ? null : new RenderContextUser(userDetail),
            RenderParam = renderParam,
            IsDevelopment = optionService.IsDevelopment,
        };

        //FrontOptions frontOpt = optionService.GetOption<FrontOptions>();
        //var hostHtml = frontOpt.HostItems.FirstOrDefault(s => s.Url == appFront.Configuration.Url)?.HostHtml!;
        //var webRoot = new WebRoot(new WebSitePart(WebSitePartType.Root, "PrepareHostHtml", "PrepareHostHtml.cs", "", hostHtml, new Dictionary<string, string>(), "PrepareHostHtml"));

        var wts = appFront.Features.Get<IWebTemplateService>();
        var webRoot = wts.Template.Roots.GetValueOrDefault(AppFront.Configuration.Url) ?? wts.Template.Roots.First().Value;

        var appFrontBlazorAsm = appFront.Features.Get<AppFrontBlazorAsm>();

        //var isRoot = serviceProvider;
        //IHtmlHelper htmlHelper = serviceProvider.GetRequiredService<IHtmlHelper>();
        //var xx = new MyBlazorHtmlHelper(htmlHelper);
        //var blazorPageHtml = await htmlHelper.RenderComponentAsync(appFrontBlazorAsm.AppType, Microsoft.AspNetCore.Mvc.Rendering.RenderMode.WebAssemblyPrerendered, null);

        //var blazorPageHtml = await RenderAsync3(httpContext, appFrontBlazorAsm.AppType);

        //var blazorPageHtml = "@Body";

        WebPage page = WebPage.Blank(blazorPageHtml, "BlazorPage", webClientRequest.Path);

        return RenderPage(appFront, pageRenderContext, webRoot, page, null, serviceProvider, cancellationToken);
    }

    string DetermineAppRuntimeDllName(string themePath)
    {
        var dir = new DirectoryInfo(themePath);
        var dllRuntimeFiles = dir.GetFiles("*.staticwebassets.runtime.json");
        if (dllRuntimeFiles.Length == 0)
            throw new FileNotFoundException("Cannot find '*.staticwebassets.runtime.json' file in theme path");
        else if (dllRuntimeFiles.Length > 1)
            throw new InvalidOperationException("Multiple '*.staticwebassets.runtime.json' files found in theme path");

        return dllRuntimeFiles[0].Name.Replace(".staticwebassets.runtime.json", ".dll");
    }

    public async Task<string> RenderAsync2(HttpContext httpContext, Type componentType, object? parameters = null)
    {

        var renderer = httpContext.RequestServices.GetRequiredService<HtmlRenderer>();

        //var htmlContent = await httpContext.RequestServices
        //.GetRequiredService<HtmlRenderer>()
        //.Dispatcher.InvokeAsync(async () =>
        //{
        //    var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        //    {
        //        { "Title", "Test" }
        //    });

        //    //var renderer = HttpContext.RequestServices.GetRequiredService<IComponentRenderer>();
        //    var result = await renderer.RenderComponentAsync(componentType, parameters);
        //    return result.ToHtmlString();
        //});

        var parameters2 = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { "Title", "Test" }
            });
        var result = await renderer.RenderComponentAsync(componentType, parameters2);

        return result.ToHtmlString();
    }

    public async Task<string> RenderAsync3(HttpContext httpContext, Type componentType, object? parameters = null)
    {
        ////var app = await Microsoft.AspNetCore.Html.RenderComponentAsync<App>(RenderMode.Static);
        //IHtmlHelper htmlHelper = serviceProvider.GetRequiredService<IHtmlHelper>();
        ////Microsoft.AspNetCore.Html.IHtmlContent html = await htmlHelper.RenderComponentAsync<App>(RenderMode.Static);
        //Microsoft.AspNetCore.Html.IHtmlContent html = await htmlHelper.RenderComponentAsync<>(Microsoft.AspNetCore.Mvc.Rendering.RenderMode.Static);

        //IViewEngine viewEngine = httpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();

        //ViewEngineResult viewResult = viewEngine.FindView(httpContext.Response, "nameof(Pages__Host)", true);

        var renderer = httpContext.RequestServices.GetRequiredService<BlazorRenderer>();

        //var html = await renderer.RenderComponent(componentType, new() { { "Title", "My title" } });
        var html = await renderer.RenderComponent(componentType);

        return html;
    }

}
