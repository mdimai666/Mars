using AppFront.Main.OptionEditForms;
using AppFront.Shared.Bridges;
using AppFront.Shared.Handlers;
using AppFront.Shared.OptionEditForms;
using AppFront.Shared.Services;
using BlazoredHtmlRender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppFront.Shared;

public static class MainAppFrontShared
{
    public static void AddAppFrontMain(this IServiceCollection services, IConfiguration configuration, Type program)
    {
        services.AddAppFront(configuration, program);
        services.AddFluentUIComponents();

        services.InstallHandlers();

        services.TryAddScoped<IAppMediaService, AppMediaService>();
        services.TryAddScoped<Interfaces.IMessageService, FluentMessageServiceBridge>();

        BlazoredHtml.AddComponentsFromAssembly(typeof(AppFront.Shared.Components.Affix).Assembly, true);
        BlazoredHtml.AddComponentsFromAssembly(typeof(FluentButton).Assembly, true);

        services.AddSingleton<OptionsFormsLocator>();
    }

    public static IServiceProvider UseAppFrontMain(this IServiceProvider services)
    {
        var optionsFormsLocator = services.GetRequiredService<OptionsFormsLocator>();
        optionsFormsLocator.RegisterAssembly(typeof(SmtpSettingsEditForm).Assembly);

        return services;
    }
}
