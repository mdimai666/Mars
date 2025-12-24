using Mars.Datasource.Core.Nodes;
using Mars.Datasource.Front.Nodes.EditForms;
using Mars.Datasource.Front.Services;
using Mars.Nodes.Core;
using Mars.Nodes.FormEditor;
using Mars.WebApiClient.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Front;

public static class MainMarsDatasourceFront
{
    public static IServiceCollection AddDatasourceWorkspace(this IServiceCollection services)
    {
        services.AddScoped<IDatasourceServiceClient, DatasourceServiceClient>();

        return services;
    }

    public static IServiceProvider UseDatasourceWorkspace(this IServiceProvider services)
    {
        var _nodeFormsLocator = services.GetRequiredService<NodeFormsLocator>();

        services.GetRequiredService<NodesLocator>().RegisterAssembly(typeof(SqlNode).Assembly);
        _nodeFormsLocator.RegisterAssembly(typeof(SqlNodeForm).Assembly);

        return services;
    }

}
