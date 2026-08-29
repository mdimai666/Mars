using Mars.Admin.Framework.OptionEditForms;
using Mars.Admin.Framework;
using EditorJsBlazored.Host;
using Mars.AiChat.Host;
using Mars.CommandLine;
using Mars.Datasource.Front;
using Mars.Datasource.Host;
using Mars.Docker.Host;
using Mars.Excel.Host;
using Mars.Server;
using Mars.Server.Abstractions.Extensions;
using Mars.Server.Abstractions.Features;
using Mars.Server.CommandLine;
using Mars.Server.Startup;
using Mars.Nodes.Abstractions.Hubs;
using Mars.Server.Abstractions.JsonConverters;
using Mars.SiteEngine.Abstractions.Services;
using Mars.Server.Abstractions.Startup;
using Mars.Identity.Host;
using Mars.Media.Host;
using Mars.Notifications.Host;
using Mars.Options.Front;
using Mars.Options.Host;
using Mars.MetaModelGenerator;
using Mars.Plugin;
using Mars.QueryLang.Host;
using Mars.Scheduler.Host;
using Mars.SemanticKernel.CMS;
using Mars.SemanticKernel.Host;
using Mars.Setup;
using Mars.SSO.Host;
using Mars.SSO.Host.OAuth;
using Mars.UseStartup;
using Mars.UseStartup.MarsParts;
using Mars.WebApp.Nodes.Host;
using Mars.XActions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FeatureManagement;
using static Mars.UseStartup.MarsStartupInfo;
using Mars.CommandLine.Abstractions;
using Mars.CommandLine.Remote;

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
        builder.Services.MarsAddLocalization()
                        .MarsAddCore(builder.Configuration)
                        .AddAspNetTools()
                        .MarsAddMetrics(builder.Configuration)
                        .AddConfigureActions()
                        .AddMarsWebSiteProcessor();
        builder.AddWREHandlebars();

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
        builder.Services.MarsAddSwagger()
                        .AddMarsOptions()
                        .AddMarsNotifications()
                        .AddMarsIdentity(builder.Configuration)
                        .AddMarsMedia()
                        .AddMarsCms()
                        .AddMarsQueryLang()
                        .AddMetaModelGenerator()
                        .AddMarsHostServices(builder.Environment)
                        .MarsAddTemplator()
                        .AddPostgresDistributedCache(builder.Configuration)
                        .AddMarsNodes()
                        .AddMarsWebAppNodes()
                        .AddDatasourceHost()
                        .AddMarsScheduler()
                        .AddMarsExcel()
                        .AddEditorJsBlazored();

        builder.AddIfFeatureEnabled(FeatureFlags.DockerAgent, b => b.Services.AddMarsDocker());
        builder.AddIfFeatureEnabled(FeatureFlags.AITool, builder =>
        {
            builder.Services.AddMarsSemanticKernel();
            builder.AddAiCmsHost();
        });
        builder.AddIfFeatureEnabled(FeatureFlags.AiChat, b => b.Services.AddMarsAiChat());
        builder.AddIfFeatureEnabled(FeatureFlags.SingleSignOn, b => b.Services.AddMarsSSO().AddMarsOAuthHost());

        //------------------------------------------
        // CLIENT
#if !NOADMIN
        builder.Services.AddAppFrontMain(builder.Configuration, typeof(Mars.Admin.App));
#endif
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
        app.Services.UseMarsHostServices();
        app.Services.UseMarsMedia();
        app.Services.UseMarsNotifications();
        app.Services.UseMarsSiteEngineOptions();
        app.Services.SeedData(builder.Configuration, _logger, migrated);
        app.Services.GetRequiredService<IFrontManager>();
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
        app.MapRazorPages();

        app.UseMarsCliSocket(Instance);

        app.MapHub<ChatHub>("/_ws/admin", options =>
        {
            options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
        });

        app.MarsUseMetrics();
        app.UseMarsHost(builder.Services);
        app.UseMarsIdentity();
        app.UseMarsOptions();
        app.UseHostFiles();
        app.UseConfigureActions();
        app.MarsUseTemplator();
        //app.UseMiddleware<Mars.Middlewares.DebugObjectsLifetimeMiddleware>();
        app.Services.UseNodeWorkspace()
                    .UseDatasourceWorkspace()
                    .UseAppFrontMain();

        var optionsFormsLocator = app.Services.GetRequiredService<IOptionsFormsLocator>();
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
        app.UseIfFeatureEnabled(FeatureFlags.AITool, app => app.UseMarsSemanticKernel().UseAiCmsHost());
        app.UseIfFeatureEnabled(FeatureFlags.AiChat, app => app.UseMarsAiChat());
        app.UseIfFeatureEnabled(FeatureFlags.SingleSignOn, app => app.ApplicationServices.UseMarsSSO().UseMarsOAuthHost());

        app.UseMarsScheduler();
        app.UseMarsSiteEngineFront();

        IMarsAppLifetimeService.UseAppLifetime(builder.Services, app);
    }

}
