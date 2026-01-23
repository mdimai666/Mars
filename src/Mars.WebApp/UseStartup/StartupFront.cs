using System.Diagnostics;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Services;
using Mars.UseStartup.MarsParts;
using Mars.WebSiteProcessor.Blazor;
using Mars.WebSiteProcessor.DatabaseHandlebars;
using Mars.WebSiteProcessor.Handlebars;
using Mars.WebSiteProcessor.Interfaces;

namespace Mars.UseStartup;

public static class StartupFront
{
    public static MarsAppProvider AppProvider = default!;

    static IOptionService optionService = default!;

    public static WebApplicationBuilder AddFront(this WebApplicationBuilder builder)
    {
        AppProvider = new MarsAppProvider(builder.Configuration);
        builder.Services.AddSingleton(AppProvider);
        builder.Services.AddSingleton<IMarsAppProvider>(sp => AppProvider);

        builder.AddWREHandlebars(AppProvider.Apps.Values);
        builder.AddWREDatabaseHandlebars(AppProvider.Apps.Values);
        builder.AddWREBlazor(AppProvider.Apps.Values);

        return builder;
    }

    [DebuggerStepThrough]
    static Task AppendMarsAppFrontInRequestContextItems(HttpContext context, Func<Task> next)
    {
        var app = AppProvider.GetAppForUrl(context.Request.Path);
        context.Items.Add(nameof(MarsAppFront), app);

        return next.Invoke();
    }

    public static IApplicationBuilder UseFront(this WebApplication app)
    {
        optionService = app.Services.GetRequiredService<IOptionService>();

        UseRobotsTxt(app);
        app.Use(AppendMarsAppFrontInRequestContextItems);

        app.UseWREHandlebars(AppProvider.Apps.Values);
        app.UseWREDatabaseHandlebars(AppProvider.Apps.Values);
        app.UseWREBlazor(AppProvider.Apps.Values);

        foreach (var appFront in AppProvider.Apps.Values.Reverse())
        {
            var renderEngine = appFront.Features.Get<IWebRenderEngine>()
                ?? throw new InvalidOperationException($"RenderEngine '{appFront.Configuration.Mode}' not resolved");

            renderEngine.Setup();

            app.Map(appFront.Configuration.Url, front =>
            {
                front.UseRouting();
                front.UseAuthorization();

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

    static void UseRobotsTxt(WebApplication app)
    {
        app.Map("robots.txt", (HttpContext context) =>
        {
            context.Response.StatusCode = 200;
            return optionService.RobotsTxt();
        }).ShortCircuit();
    }

}
