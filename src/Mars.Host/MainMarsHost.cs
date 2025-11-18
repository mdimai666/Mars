using System.Reflection;
using FluentValidation;
using Mars.Host.Handlers;
using Mars.Host.Managers;
using Mars.Host.QueryLang;
using Mars.Host.Services;
using Mars.Host.Services.GallerySpace;
using Mars.Host.Shared.Attributes;
using Mars.Host.Shared.Constants.Website;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Handlers;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Host.Shared.WebSite.Scripts;
using Mars.Host.WebSite.Scripts;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MOptions = Microsoft.Extensions.Options.Options;

namespace Mars.Host;

public static class MainMarsHost
{
    public static IServiceCollection AddMarsHost(this IServiceCollection services, IWebHostEnvironment wenv)
    {
        services.AddSingleton<IActionManager, XActionManager>();
        services.AddSingleton<IOptionService, OptionService>();
        services.AddSingleton<IEventManager, EventManager>();
        services.AddSingleton<INavMenuService, NavMenuService>();
        services.AddSingleton<IMetaModelTypesLocator, MetaModelTypesLocator>();

        services.AddSingleton<IActionHistoryService, ActionHistoryService>();
        //services.AddSingleton<ModelInfoService>(); // Mars\Mars.Shared\Tools\ModelInfoService.cs

        services.AddLocalization();
        services.AddSingleton<IStringLocalizer<AppRes>, StringLocalizer<AppRes>>();
        services.AddSingleton(sp => sp.GetRequiredService<StringLocalizer<AppRes>>());

        services.AddTransient<IMarsEmailSender, EmailSender>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddTransient<ISmsSender, SmsSender>();
        services.AddTransient<INotifyService, NotifyService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserTypeService, UserTypeService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IPostTypeService, PostTypeService>();
        services.AddScoped<IPostJsonService, PostJsonService>();
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddScoped<IFeedbackService, FeedbackService>();

        services.AddScoped<InitialSiteDataViewModelHandler>();
        services.AddScoped<IGalleryService, GalleryService>();
        services.AddScoped<IMetaFieldMaterializerService, MetaFieldMaterializerService>();
        services.AddScoped<ICentralSearchService, CentralSearchService>();
        services.AddScoped<IFaviconGeneratorHandler, FaviconGeneratorHandler>();
        services.AddScoped<SiteFaviconConfiguratorHandler>();

        ValidatorFabric.AddValidatorsFromAssembly(services, typeof(CreatePostQueryValidator).Assembly);

        UseFileStorages(services, wenv);

        //read (may object viewer)
        // Microsoft.AspNetCore.Identity.IEmailSender

        services.AddValidatorsFromAssemblyContaining<UpdatePostQueryValidator>();
        services.AddScoped<IValidatorFabric, ValidatorFabric>();

        UseIMetaRelationModelProviderHandler(services);
        RegisterAIToolScenarioProviders(services);
        services.AddScoped<IPostTransformer, PostTransformer>();
        RegisterPostContentProcessorsLocator(services);

        AddSiteScriptsBuilders(services);

        return services;
    }

    public static IApplicationBuilder UseMarsHost(this WebApplication app, IServiceCollection serviceCollection)
    {
        UseSiteScriptsBuilders(app.Services);

        return app;
    }

    static void UseIMetaRelationModelProviderHandler(IServiceCollection services)
    {
        services
            .AddKeyedScoped<IMetaRelationModelProviderHandler, UserRelationModelProviderHandler>("User")
            .AddKeyedScoped<IMetaRelationModelProviderHandler, FileRelationModelProviderHandler>("File")
            .AddKeyedScoped<IMetaRelationModelProviderHandler, PostRelationModelProviderHandler>("Post")
            .AddKeyedScoped<IMetaRelationModelProviderHandler, FeedbackRelationModelProviderHandler>("Feedback")
            .AddKeyedScoped<IMetaRelationModelProviderHandler, NavMenuRelationModelProviderHandler>("NavMenu")
            ;
    }

    static void UseFileStorages(IServiceCollection services, IWebHostEnvironment wenv)
    {
        services.AddSingleton<IFileStorage, FileStorage>();
        services.AddSingleton<IOptions<FileHostingInfo>>(sp => MOptions.Create(sp.GetRequiredService<IOptionService>().FileHostingInfo()));

        var dataDirHostingInfo = MOptions.Create(new FileHostingInfo()
        {
            Backend = null,
            PhysicalPath = new Uri(Path.Combine(wenv.ContentRootPath, "data"), UriKind.Absolute),
            RequestPath = ""
        });

        var dataFs = new FileStorage(dataDirHostingInfo);

        services.AddKeyedSingleton<IOptions<FileHostingInfo>>("data", dataDirHostingInfo);
        services.AddKeyedSingleton<IFileStorage>("data", dataFs);
    }

    static IServiceCollection RegisterAIToolScenarioProviders(this IServiceCollection services)
    {
        services.AddSingleton<IAIToolScenarioProvidersLocator, AIToolScenarioProvidersLocator>();
        //var toolMap = new Dictionary<string, Type>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location));

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (!type.IsClass || type.IsAbstract)
                continue;

            var attr = type.GetCustomAttribute<RegisterAIToolAttribute>();
            if (attr == null)
                continue;

            if (typeof(IAIToolScenarioProvider).IsAssignableFrom(type))
            {
                var key = attr.Key ?? type.Name;
                services.AddKeyedTransient(typeof(IAIToolScenarioProvider), key, type);
            }

            //toolMap[attr.Key] = type;
        }

        return services;
    }

    static IServiceCollection RegisterPostContentProcessorsLocator(this IServiceCollection services)
    {
        services.AddSingleton<IPostContentProcessorsLocator, PostContentProcessorsLocator>();
        //var toolMap = new Dictionary<string, Type>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location));

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (!type.IsClass || type.IsAbstract)
                continue;

            var attr = type.GetCustomAttribute<KeyredHandlerAttribute>();
            if (attr == null)
                continue;

            if (typeof(IPostContentProcessor).IsAssignableFrom(type))
            {
                var key = attr.Key ?? type.Name;
                services.AddKeyedScoped(typeof(IPostContentProcessor), key, type);
            }

            //toolMap[attr.Key] = type;
        }

        return services;
    }

    static void AddSiteScriptsBuilders(IServiceCollection services)
    {
        services.AddKeyedSingleton<ISiteScriptsBuilder, SiteScriptsBuilder>(AppAdminConstants.SiteScriptsBuilderKey);
        services.AddKeyedSingleton<ISiteScriptsBuilder, SiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);

        services.AddKeyedSingleton<IWebSitePluggablePluginScripts, AppAdminWebSitePluggablePluginScripts>(AppAdminConstants.SiteScriptsBuilderKey);
        services.AddKeyedSingleton<IWebSitePluggablePluginScripts, AppFrontWebSitePluggablePluginScripts>(AppFrontConstants.SiteScriptsBuilderKey);
    }

    static void UseSiteScriptsBuilders(IServiceProvider serviceProvider)
    {
        //AppAdmin
        {
            // core
            var appAdminBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppAdminConstants.SiteScriptsBuilderKey);
            appAdminBuilder.RegisterProvider("favicon", new FaviconAssetProvider(serviceProvider.GetRequiredService<IOptionService>()), order: 8f, placeInHead: true);
            var appAdminSpaHtmlScripts = new AppAdminSpaHtmlScripts();
            appAdminBuilder.RegisterProvider("appadmin_head", new AppAdminHeadAssetProvider(appAdminSpaHtmlScripts), order: 9f, placeInHead: true);
            appAdminBuilder.RegisterProvider("appadmin_footer", new AppAdminFooterAssetProvider(appAdminSpaHtmlScripts), order: 9f, placeInHead: false);

            // pluggable
            var appAdminWebSitePluggablePluginScripts = serviceProvider.GetRequiredKeyedService<IWebSitePluggablePluginScripts>(AppAdminConstants.SiteScriptsBuilderKey);
            appAdminBuilder.RegisterProvider("appadmin_scripts_head", new WebSitePluggableHeaderAssetProvider(appAdminWebSitePluggablePluginScripts), order: 10, placeInHead: true);
            appAdminBuilder.RegisterProvider("appadmin_scripts_footer", new WebSitePluggableFooterAssetProvider(appAdminWebSitePluggablePluginScripts), order: 10, placeInHead: false);
        }

        //AppFront
        {
            // core
            var appFrontBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);
            appFrontBuilder.RegisterProvider("favicon", new FaviconAssetProvider(serviceProvider.GetRequiredService<IOptionService>()), order: 9f, placeInHead: true);

            // pluggable
            var appFrontWebSitePluggablePluginScripts = serviceProvider.GetRequiredKeyedService<IWebSitePluggablePluginScripts>(AppFrontConstants.SiteScriptsBuilderKey);
            appFrontBuilder.RegisterProvider("appfront_scripts_head", new WebSitePluggableHeaderAssetProvider(appFrontWebSitePluggablePluginScripts), order: 10, placeInHead: true);
            appFrontBuilder.RegisterProvider("appfront_scripts_footer", new WebSitePluggableFooterAssetProvider(appFrontWebSitePluggablePluginScripts), order: 10, placeInHead: false);
        }

    }
}
