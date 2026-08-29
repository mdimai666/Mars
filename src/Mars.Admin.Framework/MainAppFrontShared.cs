using Mars.Admin.Framework.OptionEditForms;
using Mars.Admin.Framework.AuthProviders;
using Mars.Admin.Framework.Bridges;
using Mars.Admin.Framework.Handlers;
using Mars.Admin.Framework.Interfaces;
using Mars.Admin.Framework.OptionEditForms;
using Mars.Admin.Framework.Services;
using Blazored.LocalStorage;
using BlazoredHtmlRender;
using Flurl.Http;
using Mars.Admin.Framework.Tools;
using Mars.WebApiClient;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework;

public static class MainAppFrontShared
{
    public static void AddAppFrontMain(this IServiceCollection services, IConfiguration configuration, Type program)
    {
        services.AddAppFront(configuration, program);
        services.AddFluentUIComponents();

        services.AddSingleton<IOptionsFormsLocator, OptionsFormsLocator>();
        services.TryAddSingleton<Mars.Admin.Framework.Components.MetaFieldViews.IMetaFieldEditorLocator,
            Mars.Admin.Framework.Components.MetaFieldViews.MetaFieldEditorLocator>();

        if (!OperatingSystem.IsBrowser()) return;

        services.InstallHandlers();

        services.TryAddScoped<IAppMediaService, AppMediaService>();
        services.TryAddScoped<IChildPostEditor, ChildPostEditorService>();
        services.TryAddScoped<Interfaces.IMessageService, FluentMessageServiceBridge>();

        BlazoredHtml.AddComponentsFromAssembly(typeof(Mars.Admin.Framework.Components.Affix).Assembly, true);
        BlazoredHtml.AddComponentsFromAssembly(typeof(FluentButton).Assembly, true);

        if (OperatingSystem.IsBrowser())
        {
            services.AddMemoryCache();
        }
    }

    public static IServiceProvider UseAppFrontMain(this IServiceProvider services)
    {
        var optionsFormsLocator = services.GetRequiredService<IOptionsFormsLocator>();
        optionsFormsLocator.RegisterAssembly(typeof(SmtpSettingsEditForm).Assembly);

        return services;
    }

    /// <summary>
    /// Серверные регистрации: двойники регистраций из <see cref="AddAppFront"/>,
    /// который на сервере не выполняется (ранний выход по !IsBrowser()).
    /// </summary>
    public static IServiceCollection AddAdminFrameworkServerServices(this IServiceCollection services)
    {
        services.TryAddSingleton<ModelInfoService>();
        services.TryAddSingleton<IBlazorPagesService, BlazorPagesService>();

        return services;
    }

    public static void AddAppFront(this IServiceCollection services, IConfiguration configuration, Type program)
    {
        if (!OperatingSystem.IsBrowser()) return;

        ArgumentNullException.ThrowIfNull(services);

        if (!services.Any(d => d.ServiceType == typeof(HttpClient))
            || !services.Any(d => d.ServiceType == typeof(IFlurlClient)))
        {
            throw new InvalidOperationException("HttpClient and IFlurlClient must be registered.");
        }

        Q.Program = program;

        services.AddBlazoredLocalStorage();
        services.AddAuthorizationCore();
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();
        services.TryAddScoped<CookieOrLocalStorageAuthStateProvider>();
        services.TryAddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CookieOrLocalStorageAuthStateProvider>());

        services.ConfigureLocalizer();

        services.TryAddScoped<ViewModelService>();
        services.TryAddScoped<AppFrontJs>();

        services.TryAddSingleton<ModelInfoService>();
        services.TryAddSingleton<IBlazorPagesService, BlazorPagesService>();
        services.TryAddScoped<DeveloperControlService>();
        //services.TryAddScoped<GalleryService>();
        services.TryAddScoped<IActAppService, ActAppService>();
        services.TryAddScoped<IXActionFormPresenter, NullXActionFormPresenter>();
        services.TryAddSingleton<IXActionFormProvider, XActionFormProvider>();
        services.TryAddScoped<IAIToolAppService, AIToolAppService>();

        services.AddMarsWebApiClient();

        //builder.Logging.SetMinimumLevel(LogLevel.Error);

        BlazoredHtml.AddComponentsFromAssembly(Q.Program.Assembly, true);
        BlazoredHtml.AddComponentsFromAssembly(typeof(Mars.Admin.Framework.Components.LikeButton).Assembly, true);
    }

    private static void ConfigureLocalizer(this IServiceCollection services)
    {
        services.AddLocalization();
        //Такое писать не требуется. Оставлено для внимания.
        //services.TryAddSingleton<IStringLocalizer, StringLocalizer<AppRes>>();
        //services.TryAddSingleton<IStringLocalizer<AppRes>, StringLocalizer<AppRes>>();
    }
}
