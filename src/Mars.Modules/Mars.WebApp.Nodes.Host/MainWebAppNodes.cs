using Mars.Nodes.Core.Implements;
using Mars.WebApp.Nodes.Front;
using Mars.WebApp.Nodes.Host.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Host;

public static class MainWebAppNodes
{
    public static IServiceCollection AddMarsWebAppNodes(this IServiceCollection services)
        => services.AddMarsWebAppNodesFront();

    public static IApplicationBuilder UseMarsWebAppNodes(this IApplicationBuilder app)
    {
        app.ApplicationServices.UseMarsWebAppNodesFront();

        var nodeImplementFabirc = app.ApplicationServices.GetRequiredService<NodeImplementFabirc>();
        nodeImplementFabirc.RegisterAssembly(typeof(ExcelNodeImplement).Assembly);
        return app;
    }

}
