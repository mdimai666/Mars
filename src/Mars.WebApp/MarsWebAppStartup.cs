using EditorJsBlazored.Host;
using Mars.Admin.Host;
using Mars.AiChat.Host;
using Mars.Cms.Host;
using Mars.CommandLine;
using Mars.CommandLine.Abstractions;
using Mars.CommandLine.Remote;
using Mars.Datasource.Front;
using Mars.Datasource.Host;
using Mars.Docker.Host;
using Mars.Excel.Host;
using Mars.Identity.Host;
using Mars.Media.Host;
using Mars.MetaModelGenerator;
using Mars.Nodes.Host;
using Mars.Nodes.Workspace;
using Mars.Notifications.Host;
using Mars.Options.Host;
using Mars.Plugin;
using Mars.QueryLang.Host;
using Mars.Scheduler.Host;
using Mars.SemanticKernel.CMS;
using Mars.SemanticKernel.Host;
using Mars.Server;
using Mars.Server.Abstractions.Extensions;
using Mars.Server.Abstractions.Features;
using Mars.Server.Abstractions.JsonConverters;
using Mars.Server.Abstractions.Services;
using Mars.Server.Abstractions.Startup;
using Mars.Server.CommandLine;
using Mars.Server.Startup;
using Mars.Setup;
using Mars.SiteEngine.Handlebars;
using Mars.SiteEngine.Host;
using Mars.SSO.Host;
using Mars.SSO.Host.OAuth;
using Mars.UseStartup;
using Mars.UseStartup.MarsParts;
using Mars.WebApp.Nodes.Host;
using Mars.XActions.Host;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FeatureManagement;
using static Mars.UseStartup.MarsStartupInfo;

namespace Mars;

public static class MarsWebAppStartup
{
    public static void ConfigureBuilder(WebApplicationBuilder builder, string[] args)
    {
        var commandsApi = new CommandLineApi(typeof(Program).Assembly, [typeof(InfoCommand), typeof(MigrationCommandCli)]);
        builder.Services.AddSingleton<ICommandLineApi>(commandsApi);

        if (!IsTesting && !IsRunningInDocker)
        {
            builder.Configuration.ConfigureAppConfiguration(args);
        }
        else if (IsRunningInDocker && !IsTesting)
        {
            // конфиг, записанный setup-визардом на том ./config; приоритет ниже env-переменных
            builder.Configuration.AddWizardConfigSource();
        }
        builder.Services.AddSingleton<IMarsStartupInfo>(MarsStartupInfo.Instance);
        builder.Services.AddFeatureManagement(builder.Configuration.GetSection(FeatureExtensions.SectionName));
        builder.Services.AddMarsLocalization()
                        .MarsAddCore(builder.Configuration)
                        .AddAspNetTools()
                        .MarsAddMetrics(builder.Configuration);

        builder.WebHost.UseStaticWebAssets();
        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.TypeInfoResolver = new OrderedPropertiesJsonTypeInfoResolver());

        builder.Services.AddMarsSignalRConfiguration()
                        .AddRazorPages();
        builder.Services.AddSingleton<SetupService>();
        //builder.Services.AddServerSideBlazor();

        //------------------------------------------
        // Logger
        bool disableLogs = commandsApi.CheckGlobalOption<bool>("--disable-logs", args)
                           || MarsCliSocket.RunningServer is not null;
        if (!disableLogs && !IsTesting)
        {
            builder.MarsAddLogging();
        }

        //------------------------------------------
        // Mars
        builder.Services.AddMarsSwagger()
                        .AddMarsOptions()
                        .AddMarsNotifications()
                        .AddMarsIdentity(builder.Configuration)
                        .AddMarsMedia()
                        .AddMarsCms()
                        .AddMarsQueryLang()
                        .AddMetaModelGenerator()
                        .AddMarsXActionsHost()
                        .AddMarsServer(builder.Environment)
                        .AddPostgresDistributedCache(builder.Configuration)
                        .AddMarsNodes()
                        .AddMarsWebAppNodes()
                        .AddDatasourceHost()
                        .AddMarsScheduler()
                        .AddMarsExcel()
                        .AddMarsSiteEngine()
                        .AddMarsSiteEngineHandlebars()
                        .AddEditorJsBlazored();

        builder.AddIfFeatureEnabled(FeatureFlags.DockerAgent, b => b.Services.AddMarsDocker());
        builder.AddIfFeatureEnabled(FeatureFlags.AITool, builder =>
        {
            builder.Services.AddMarsSemanticKernel();
            builder.AddMarsAiCms();
        });
        builder.AddIfFeatureEnabled(FeatureFlags.AiChat, b => b.Services.AddMarsAiChat());
        builder.AddIfFeatureEnabled(FeatureFlags.SingleSignOn, b => b.Services.AddMarsSSO().AddMarsOAuth());

        //------------------------------------------
        // CLIENT
        builder.Services.AddMarsAdmin(builder.Configuration);
        builder.Services.AddNodeWorkspace();
        builder.Services.AddDatasourceWorkspace();
        // end CLIENT

        //------------------------------------------
        // PLUGINS
        builder.AddPlugins();
        builder.AddMarsCliSocket(commandsApi, args, Instance);
    }

    public static async Task ConfigureApp(WebApplication app, WebApplicationBuilder builder, string[] args)
    {
        var _logger = app.Services.GetRequiredService<ILogger<Program>>();
        MarsLogger.Initialize(app.Services.GetRequiredService<ILoggerFactory>()); // use like: MarsLogger.GetStaticLogger<T>().LogError(...)
        var env = app.Environment;
        //var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        // Normal startup — config must exist (wizard runs before main app)
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");
        if (!IsTesting && !IsRunningInDocker && !File.Exists(configPath))
        {
            throw new InvalidOperationException("appsettings.Local.json not found. Setup wizard should have run before main application.");
        }

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
        app.Services.UseMarsServerOptions();
        app.Services.SeedData(builder.Configuration, _logger, migrated);

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseMarsLocalization();

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
        app.UseIfFeatureEnabled(FeatureFlags.SingleSignOn, app => app.UseMarsSSO());
#pragma warning disable ASP0001 // Authorization middleware is incorrectly configured
        app.UseAuthorization();
#pragma warning restore ASP0001 // Authorization middleware is incorrectly configured

        app.UseMarsSwagger();
        app.MapControllers();
        app.MapRazorPages();

        app.UseMarsCliSocket(Instance);

        app.MarsUseMetrics();

        app.UseMarsIdentity();
        app.UseMarsOptions();
        app.Services.UseMarsNotifications();

        app.UseStaticFiles();

        app.UseMarsServer();
        app.UseMarsCms();
        app.UseMarsMedia();

        app.Services.UseNodeWorkspace()
                    .UseDatasourceWorkspace();

        app.UsePlugins();
        app.UseMarsAdmin();
        app.UseMarsNodes()
           .UseMarsWebAppNodes();
        app.UseDatasourceHost();
        app.UseEditorJsBlazored();
        app.Services.UseMarsSiteEngineStartup();
        //app.UseMiddleware<Mars.Middlewares.DebugObjectsLifetimeMiddleware>();

        app.UseIfFeatureEnabled(FeatureFlags.AITool, app => app.UseMarsSemanticKernel());
        app.UseIfFeatureEnabled(FeatureFlags.AiChat, app => app.UseMarsAiChat());
        app.UseIfFeatureEnabled(FeatureFlags.SingleSignOn, app => app.ApplicationServices.UseMarsOAuth());

        app.UseMarsSiteEngine();

        app.ApplyPluginMigrations();

        IMarsAppLifetimeService.UseAppLifetime(builder.Services, app);
    }

}
