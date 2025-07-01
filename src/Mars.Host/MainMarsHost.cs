using FluentValidation;
using Mars.Host.Handlers;
using Mars.Host.Managers;
using Mars.Host.QueryLang;
using Mars.Host.Services;
using Mars.Host.Services.GallerySpace;
using Mars.Host.Services.Keycloak;
using Mars.Host.Services.MarsSSOClient;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
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
        services.AddSingleton<IActionManager, ActionManager>();
        services.AddSingleton<IOptionService, OptionService>();
        services.AddSingleton<IEventManager, EventManager>();
        services.AddSingleton<INavMenuService, NavMenuService>();
        services.AddSingleton<IMetaModelTypesLocator, MetaModelTypesLocator>();

        services.AddSingleton<IActionHistoryService, ActionHistoryService>();
        //services.AddSingleton<ModelInfoService>(); //C:\Users\D\Documents\VisualStudio\2025\Mars\Mars.Shared\Tools\ModelInfoService.cs

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

        services.AddScoped<ViewModelService>();
        services.AddScoped<EsiaService>();

        services.AddScoped<KeycloakService>();
        services.AddScoped<MarsSSOClientService>();
        services.AddScoped<MarsSSOOpenIDServerService>();

        services.AddScoped<IGalleryService, GalleryService>();
        services.AddScoped<IMetaFieldMaterializerService, MetaFieldMaterializerService>();
        services.AddScoped<ICentralSearchService, CentralSearchService>();

        //temp
        services.AddScoped<PostTypeExporter>();

        UseFileStorages(services, wenv);

        //read (may object viewer)
        // Microsoft.AspNetCore.Identity.IEmailSender

        services.AddValidatorsFromAssemblyContaining<UpdatePostQueryValidator>();
        services.AddScoped<IValidatorFabric, ValidatorFabric>();

        UseIMetaRelationModelProviderHandler(services);

        return services;
    }

    public static IApplicationBuilder UseMarsHost(this WebApplication app, IServiceCollection serviceCollection)
    {
        ValidatorFabric.Initialize(serviceCollection);

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
}
