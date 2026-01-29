using AppFront.Main.OptionEditForms;
using AppFront.Shared;
using EditorJsBlazored.Host;
using Mars.CommandLine;
using Mars.Datasource.Front;
using Mars.Datasource.Host;
using Mars.Docker.Host;
using Mars.Excel.Host;
using Mars.Host;
using Mars.Host.Shared.Extensions;
using Mars.Host.Shared.Features;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Mars.Nodes.Host;
using Mars.Nodes.Workspace;
using Mars.Options.Front;
using Mars.Options.Host;
using Mars.Plugin;
using Mars.Scheduler.Host;
using Mars.SemanticKernel.Host;
using Mars.SSO;
using Mars.SSO.Host.OAuth;
using Mars.UseStartup;
using Mars.UseStartup.MarsParts;
using Mars.WebApp.Nodes.Host;
using Mars.WebSiteProcessor;
using Mars.XActions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FeatureManagement;
using static Mars.UseStartup.MarsStartupInfo;

namespace Mars;

public static class MarsWebAppStartup
{
    public static void ConfigureBuilder(WebApplicationBuilder builder, string[] args)
    {
        var commandsApi = new CommandLineApi();

        if (!IsTesting && !IsRunningInDocker)
        {
            builder.Configuration.ConfigureAppConfiguration(args);
        }
        builder.Services.AddFeatureManagement(builder.Configuration.GetSection(FeatureExtensions.SectionName));
        builder.Services.MarsAddLocalization()
                        .MarsAddCore(builder.Configuration)
                        .AddAspNetTools()
                        .MarsAddMetrics(builder.Configuration)
                        .AddConfigureActions()
                        .AddMarsWebSiteProcessor();
        builder.AddFront();

        builder.WebHost.UseStaticWebAssets();
        builder.Services.AddControllers();

        builder.Services.AddMarsSignalRConfiguration()
                        .AddRazorPages();
        //builder.Services.AddServerSideBlazor();

        //------------------------------------------
        // Logger
        bool disableLogs = commandsApi.CheckGlobalOption<bool>("--disable-logs", args);
        if (!disableLogs && !IsTesting)
        {
            builder.MarsAddLogging();
        }

        //------------------------------------------
        // Mars
        builder.Services.MarsAddSwagger()
                        .AddMarsOptions()
                        .AddMarsHostServices(builder.Environment)
                        .MarsAddTemplator()
                        .AddDevAdmin()
                        .AddMarsNodes()
                        .AddMarsWebAppNodes()
                        .AddDatasourceHost()
                        .AddMarsScheduler()
                        .AddMarsExcel()
                        .AddEditorJsBlazored();

        builder.AddIfFeatureEnabled(FeatureFlags.DockerAgent, b => b.Services.AddMarsDocker());
        builder.AddIfFeatureEnabled(FeatureFlags.AITool, b => b.Services.AddMarsSemanticKernel());
        builder.AddIfFeatureEnabled(FeatureFlags.SingleSignOn, b => b.Services.AddMarsSSO().AddMarsOAuthHost());

        //------------------------------------------
        // CLIENT
#if !NOADMIN
        builder.Services.AddAppFrontMain(builder.Configuration, typeof(AppAdmin.App));
#endif
        builder.Services.AddNodeWorkspace();
        builder.Services.AddDatasourceWorkspace();
        // end CLIENT

        //------------------------------------------
        // PLUGINS
        builder.AddPlugins();
    }

    public static async Task ConfigureApp(WebApplication app, WebApplicationBuilder builder, string[] args)
    {
        var _logger = app.Services.GetRequiredService<ILogger<Program>>();
        MarsLogger.Initialize(app.Services.GetRequiredService<ILoggerFactory>()); // use like: MarsLogger.GetStaticLogger<T>().LogError(...)
        var env = app.Environment;
        //var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
            app.UseWebAssemblyDebugging();

        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            //app.UseHsts();
        }

        app.Services.MarsAutoMigrateCheck(builder.Configuration, _logger, out var migrated);
        app.Services.UseMarsHostServices();
        app.Services.UseMarsOptions();
        app.Services.SeedData(builder.Configuration, _logger, migrated);
        app.ApplyPluginMigrations();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.MarsUseLocalization();

        if (!IsDevelopment)
        {
            app.UseResponseCompression();
            app.UseResponseCaching();
        }

        app.UseCors();
        //app.UseIdentityServer();
        app.UseRouting();
        //app.UseAntiforgery();
        app.UseAuthentication();
        app.UseIfFeatureEnabled(FeatureFlags.SingleSignOn, app => app.UseMarsSSOMiddlewares());
#pragma warning disable ASP0001 // Authorization middleware is incorrectly configured
        app.UseAuthorization();
#pragma warning restore ASP0001 // Authorization middleware is incorrectly configured

        app.MarsUseSwagger();
        app.MapControllers();

        app.Map("/_ws", ws =>
        {
            ws.UseRouting();

            ws.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ChatHub>("/ws", options =>
                {
                    options.Transports =
                        HttpTransportType.WebSockets |
                        HttpTransportType.LongPolling;
                });
            });
        });

        app.MarsUseMetrics();
        app.UseMarsHost(builder.Services);
        app.UseHostFiles();
        app.UseConfigureActions();
        app.MarsUseTemplator();
        //app.UseMiddleware<Mars.Middlewares.DebugObjectsLifetimeMiddleware>();
        app.Services.UseNodeWorkspace()
                    .UseDatasourceWorkspace()
                    .UseAppFrontMain();

        var optionsFormsLocator = app.Services.GetRequiredService<OptionsFormsLocator>();
        optionsFormsLocator.RegisterAssembly(typeof(ApiOptionEditForm).Assembly);

        app.UsePlugins();
        app.UseDevAdmin();
        app.UseMarsNodes()
           .UseMarsWebAppNodes();
        app.UseDatasourceHost();
        app.UseMarsWebSiteProcessor();
        app.UseMarsExcel();
        app.UseEditorJsBlazored();
        app.UseIfFeatureEnabled(FeatureFlags.DockerAgent, app => app.UseMarsDocker());
        app.UseIfFeatureEnabled(FeatureFlags.AITool, app => app.UseMarsSemanticKernel());
        app.UseIfFeatureEnabled(FeatureFlags.SingleSignOn, app => app.ApplicationServices.UseMarsSSO().UseMarsOAuthHost());

        app.UseMarsScheduler();
        app.UseFront();

        IMarsAppLifetimeService.UseAppLifetime(builder.Services, app);
    }

}
