using System.Reflection;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.WebSiteProcessor.DatabaseHandlebars;
using Mars.WebSiteProcessor.Interfaces;
using Mars.WebSiteProcessor.Services;
using Mars.WebSiteProcessor.WebSite;
using Mars.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Mars.WebSiteProcessor.Blazor;

public class BlazorWebRenderEngine : DatabaseHandlebarsWebRenderEngine, IWebRenderEngine
{
    MarsAppFront appFront = default!;

    public override void AddFront(WebApplicationBuilder builder, MarsAppFront cfg)
    {
        appFront = cfg;

        var appFrontConfig = appFront.Configuration;
        var mode = appFrontConfig.Mode;

        var APP_wwwroot = Path.Combine(appFrontConfig.Path);
        var APP_path = Path.Combine(appFrontConfig.Path, "_framework");
        var APP_name = "";

        if (mode is AppFrontMode.ServeStaticBlazor or AppFrontMode.BlazorPrerender)
        {
            if (string.IsNullOrEmpty(APP_wwwroot))
            {
                throw new ArgumentException("APP_wwwroot must set");
            }

            if (mode is AppFrontMode.BlazorPrerender && string.IsNullOrEmpty(APP_path))
            {
                throw new ArgumentException("APP_path must set");
            }
        }

        if (mode == AppFrontMode.ServeStaticBlazor)
        {


        }
        else if (mode == AppFrontMode.BlazorPrerender)
        {
            //this.APP_path = @"C:\Users\D\Documents\VisualStudio\2025\Mars\AppFront\bin\Debug\net6.0";
            APP_name = @"AppFront";

            //this.APP_path = @"C:\Users\d\Documents\Projects\2022\Mars\AppFront\bin\Debug\net6.0\publish\wwwroot\_framework";
            //this.APP_wwwroot = @"C:\Users\d\Documents\Projects\2022\Mars\AppFront\bin\Debug\net6.0\publish\wwwroot";
            //this.APP_name = @"AppFront";

            //https://stackoverflow.com/questions/1137781/correct-way-to-load-assembly-find-class-and-call-run-method
            //////string dd = @"C:\Users\d\Documents\Projects\2022\Mars\AppFront\bin\Debug\net6.0\publish\wwwroot";
            ////string dd = @"C:\Users\d\Documents\Projects\2022\Mars\AppFront\bin\Debug\net6.0\publish\";

            string extraPath = APP_path;

            //https://stackoverflow.com/a/1373295/6723966
            AppDomain.CurrentDomain.AssemblyResolve += (object? sender, ResolveEventArgs args) =>
            {
                string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
                //if (!File.Exists(assemblyPath)) return null;
                if (!File.Exists(assemblyPath))
                {
                    string assemblyPath2 = Path.Combine(extraPath, new AssemblyName(args.Name).Name + ".dll");
                    if (!File.Exists(assemblyPath2)) return null;
                    else assemblyPath = assemblyPath2;
                }
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            };

            var loadedAsms = AppDomain.CurrentDomain.GetAssemblies();
            var loadedAsmsNames = loadedAsms.Select(s => s.GetName().Name);

            var asm = Assembly.LoadFile(Path.Combine(APP_path, APP_name + ".dll"));
            //var depends = asm.GetReferencedAssemblies();

            //List<AssemblyName> extraDepends = new();

            //foreach(var a in depends)
            //{
            //    var alreadyLoaded = loadedAsmsNames.Contains(a.Name);

            //    if (!alreadyLoaded)
            //    {
            //        extraDepends.Add(a);
            //    }
            //}
            //foreach(var a in extraDepends)
            //{
            //    Assembly.LoadFile(Path.Combine(APP_path, a.Name + ".dll"));
            //}

            //AppDomain.CurrentDomain.Load(asm.GetName());
            //Assembly.LoadFile(Path.Combine(APP_path, "ChatBotFrame" + ".dll"));


            var appType = asm.GetType(APP_name + ".App");
            var programType = asm.GetType(APP_name + ".Program");
            //System.Runtime.Loader.AssemblyLoadContext.GetLoadContext()

            //IsPrerender
            FieldInfo? piShared = programType.GetField("IsPrerender");
            piShared?.SetValue(null, true);


            //AppFrontInstance.AppType = typeof(AppFront.App);
            //AppSharedSettings.Program = typeof(AppFront.Program);

            ////string path1 = @"C:\Users\d\Documents\Projects\2022\Mars\Mars\bin\Debug\net6.0"; ;
            ////string relPath = Path.GetRelativePath(dd, path1);
            //AppFrontInstance.FilesPath = "/" + relPath.Replace("\\", "/");

            AppFrontInstance.AppType = appType;
            //AppSharedSettings.Program = programType;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public override void UseFront(IApplicationBuilder app)
    {
        Initialize(appFront, app.ApplicationServices);

        var front = app;
        var appFrontConfig = appFront.Configuration;
        var APP_wwwroot = Path.Combine(appFrontConfig.Path);

        front.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(APP_wwwroot),
            //RequestPath = new PathString("/app"),
            ServeUnknownFileTypes = true
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapFallbackToPage("/_Host");
        });
    }

    protected override void Initialize(MarsAppFront appFront, IServiceProvider rootServices)
    {
        this.appFront = appFront;

        var hub = rootServices.GetRequiredService<IHubContext<ChatHub>>();

        //var scope = rootServices.CreateScope();
        var eventManager = rootServices.GetRequiredService<IEventManager>();

        //WebDatabaseTemplateService wts = new WebDatabaseTemplateService(rootServices, hub, appFront);
        WebDatabaseTemplateService wts = new WebDatabaseTemplateService(rootServices, hub, appFront, eventManager);
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
