using System.Diagnostics;
using System.Reflection;
using Mars.Core.Models;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.Host.Templators.HandlebarsFunc;
using Mars.Localizer.Services;
using Mars.Services;
using Mars.UseStartup.MarsParts;
using Mars.WebSiteProcessor.Blazor;
using Mars.WebSiteProcessor.DatabaseHandlebars;
using Mars.WebSiteProcessor.Handlebars;
using Mars.WebSiteProcessor.Interfaces;
using Mars.WebSiteProcessor.WebSite;

namespace Mars.UseStartup;

public static class StartupFront
{

    public static MarsAppFront FirstAppFront => AppProvider.FirstApp;

    public static MarsAppProvider AppProvider = default!;

    static IOptionService optionService = default!;

    public static WebApplicationBuilder AddFront(this WebApplicationBuilder builder)
    {

        MarsAppProvider appProvider = new(builder.Configuration);
        AppProvider = appProvider;

        builder.Services.AddSingleton(appProvider);
        builder.Services.AddSingleton<IMarsAppProvider>(sp => appProvider);

        builder.Services.AddTransient<IMarsHtmlTemplator, MyHandlebars>();

        foreach (var _appFront in AppProvider.Apps.Values)
        {
            var appFront = _appFront;
            var appFrontConfig = appFront.Configuration;

            var mode = appFrontConfig.Mode;

            //if (mode == AppFrontMode.None) continue;

            var renderEngine = ResolveRenderEngine(appFrontConfig.Mode);
            appFront.Features.Set<IWebRenderEngine>(renderEngine);
            renderEngine.AddFront(builder, appFront);

            string localizeFile = Path.Combine(appFrontConfig.Path, "Resources", "AppRes.resx");

            if (mode == AppFrontMode.HandlebarsTemplateStatic || mode == AppFrontMode.HandlebarsTemplate)
            {
                if (File.Exists(localizeFile))
                {
                    var resLoaderFactory = new LocalizerXmlResLoaderFactory(localizeFile);
                    builder.Services.AddSingleton<IAppFrontLocalizer>(resLoaderFactory);
                }
            }

            if (false)
            {

                //if (mode == AppFrontMode.HandlebarsTemplateStatic || mode == AppFrontMode.HandlebarsTemplate)
                //{
                //    builder.AddStaticHandlebarsFront(appFrontConfig);

                //    string localizeFile = Path.Combine(appFrontConfig.Path, "Resources", "AppRes.resx");

                //    if (File.Exists(localizeFile))
                //    {
                //        LocalizerXmlResLoaderFactory resLoaderFactory = new LocalizerXmlResLoaderFactory(localizeFile);
                //        builder.Services.AddSingleton<IAppFrontLocalizer>(resLoaderFactory);
                //    }

                //    return builder;
                //}

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
                    FieldInfo piShared = programType.GetField("IsPrerender");
                    piShared.SetValue(null, true);

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
                    //AppSharedSettings.Program = programType;
                }
            }
        }
        return builder;
    }

    [DebuggerStepThrough]
    static Task AppendMarsAppFrontInRequestContextItems(HttpContext context, Func<Task> next)
    {
        MarsAppFront app = AppProvider.GetAppForUrl(context.Request.Path);
        context.Items.Add(nameof(MarsAppFront), app);
        //context.Features.Set(app);

        return next.Invoke();
    }

    public static IApplicationBuilder UseFront(this WebApplication app)
    {
        optionService = app.Services.GetRequiredService<IOptionService>();

        UseRobotsTxt(app);

        app.Use(AppendMarsAppFrontInRequestContextItems);

        foreach (var _appFront in AppProvider.Apps.Values.Reverse())
        {
            var appFront = _appFront;
            var appFrontConfig = appFront.Configuration;
            var APP_wwwroot = Path.Combine(appFrontConfig.Path);

            var renderEngine = appFront.Features.Get<IWebRenderEngine>();

            app.Map(appFront.Configuration.Url, front =>
            {
                front.UseRouting();
                front.UseAuthorization();
                //front.UsePathBase("/app");
                //front.UseBlazorFrameworkFiles("/app");
                front.UseStaticFiles();

                renderEngine.UseFront(front);

                front.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MarsUseEndpointApiFallback();
                });

            });

        }

        return app;
    }

    static IWebRenderEngine ResolveRenderEngine(AppFrontMode mode)
    {
        return mode switch
        {
            AppFrontMode.None => new DatabaseHandlebarsWebRenderEngine(),

            AppFrontMode.HandlebarsTemplateStatic => new HandlebarsWebRenderEngine(),
            AppFrontMode.HandlebarsTemplate => new DatabaseHandlebarsWebRenderEngine(),
            AppFrontMode.BlazorPrerender => new BlazorWebRenderEngine(),
            AppFrontMode.ServeStaticBlazor => new BlazorWebRenderEngine(),
            _ => throw new NotImplementedException("RenderEngine not implement")
        };
    }

    static void UseRobotsTxt(WebApplication app)
    {
        app.Map("robots.txt", delegate (HttpContext context)
        {
            context.Response.StatusCode = 200;
            return optionService.RobotsTxt();
        }).ShortCircuit();
    }

    //static void AddEngines(IServiceCollection services)
    //{
    //    var engines = new Dictionary<AppFrontMode, Type>()
    //    {
    //        //[AppFrontMode.None] = typeof(DatabaseHandlebarsWebRenderEngine),
    //        [AppFrontMode.HandlebarsTemplateStatic] = typeof(HandlebarsWebRenderEngine),
    //        [AppFrontMode.HandlebarsTemplate] = typeof(DatabaseHandlebarsWebRenderEngine),
    //        [AppFrontMode.BlazorPrerender] = typeof(BlazorWebRenderEngine),
    //        [AppFrontMode.ServeStaticBlazor] = typeof(BlazorWebRenderEngine),
    //    };
    //    foreach (var engine in engines)
    //    {
    //        IWebRenderEngine.RenderEnginesLocator.Add(engine.Key.ToString(), engine.Value);
    //        services.Add
    //    }
    //}
}
