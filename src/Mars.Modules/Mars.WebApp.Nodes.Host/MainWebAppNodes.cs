using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.WebApp.Nodes.Host.Builders;
using Mars.WebApp.Nodes.Host.Nodes;
using Mars.WebApp.Nodes.Host.Services;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Host;

public static class MainWebAppNodes
{
    public static IServiceCollection AddMarsWebAppNodes(this IServiceCollection services)
        => services.AddSingleton<NodeEditorToolServce>()
                    .AddScoped<IAppEntityFormBuilderFactory, AppEntityFormBuilderFactory>();

    public static IApplicationBuilder UseMarsWebAppNodes(this IApplicationBuilder app)
    {
        // фронтовые хуки (INodeFormsLocator, клиент NodeEditorTool) вызывает
        // WASM-приложение админки; сервер регистрирует только модели нод
        app.ApplicationServices.GetRequiredService<INodesLocator>().RegisterAssembly(typeof(ExcelNode).Assembly);

        var nodeImplementFactory = app.ApplicationServices.GetRequiredService<INodeImplementFactory>();
        nodeImplementFactory.RegisterAssembly(typeof(ExcelNodeImplement).Assembly);
        return app;
    }

}
