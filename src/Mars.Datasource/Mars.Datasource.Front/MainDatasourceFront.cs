using Mars.Datasource.Front.Nodes.EditForms;
using Mars.Datasource.Front.Services;
using Mars.Datasource.Nodes;
using Mars.Nodes.Core;
using Mars.Nodes.FormEditor;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Front;

public static class MainDatasourceFront
{
    public static IServiceCollection AddDatasourceWorkspace(this IServiceCollection services)
    {
        if (!OperatingSystem.IsBrowser()) return services;

        services.AddScoped<IDatasourceServiceClient, DatasourceServiceClient>();

        return services;
    }

    public static IServiceProvider UseDatasourceWorkspace(this IServiceProvider services)
    {
        // На сервере ассембли нод регистрирует MainDatasource; здесь идемпотентный довызов для WASM
        services.GetRequiredService<INodesLocator>().RegisterAssembly(typeof(SqlNode).Assembly);

        if (!OperatingSystem.IsBrowser()) return services;

        var _nodeFormsLocator = services.GetRequiredService<INodeFormsLocator>();
        _nodeFormsLocator.RegisterAssembly(typeof(SqlNodeForm).Assembly);

        return services;
    }

}
