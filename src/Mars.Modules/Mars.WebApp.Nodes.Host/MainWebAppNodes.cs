using Mars.Nodes.Core.Implements;
using Mars.WebApp.Nodes.Front;
using Mars.WebApp.Nodes.Host.Builders;
using Mars.WebApp.Nodes.Host.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Host;

public static class MainWebAppNodes
{
    public static IServiceCollection AddMarsWebAppNodes(this IServiceCollection services)
        => services.AddMarsWebAppNodesFront()
                    .AddSingleton<IAppEntityFormBuilderFactory, AppEntityFormBuilderFactory>();

    public static IApplicationBuilder UseMarsWebAppNodes(this IApplicationBuilder app)
    {
        app.ApplicationServices.UseMarsWebAppNodesFront();

        var nodeImplementFactory = app.ApplicationServices.GetRequiredService<NodeImplementFactory>();
        nodeImplementFactory.RegisterAssembly(typeof(ExcelNodeImplement).Assembly);
        return app;
    }

}
