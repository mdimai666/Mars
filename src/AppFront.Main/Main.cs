using AppFront.Shared.Bridges;
using AppFront.Shared.Handlers;
using AppFront.Shared.Services;
using BlazoredHtmlRender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppFront.Shared;

public static class AppFrontSharedMainExtensions
{
    public static void AddAppFrontMain(this IServiceCollection services, IConfiguration configuration, Type program)
    {
        services.AddAppFront(configuration, program);
        services.AddFluentUIComponents();
        services.AddDataGridEntityFrameworkAdapter();

        services.InstallHandlers();

        services.TryAddScoped<IAppMediaService, AppMediaService>();
        services.TryAddScoped<Interfaces.IMessageService, FluentMessageServiceBridge>();

        BlazoredHtml.AddComponentsFromAssembly(typeof(AppFront.Shared.Components.Affix).Assembly, true);
        //BlazoredHtml.AddComponentsFromAssembly(typeof(AntDesign.Button).Assembly, true);
    }
}
