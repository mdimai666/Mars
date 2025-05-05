using Mars.Datasource.Core.Nodes;
using Mars.Datasource.Front.Nodes.EditForms;
using Mars.Datasource.Front.Services;
using Mars.Nodes.Core;
using Mars.WebApiClient.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Front;

public class Main
{

}

public static class MarsDatasourceExtensions
{
    public static IServiceCollection DatasourceWorspace(this IServiceCollection services)
    {
        services.AddScoped<IDatasourceServiceClient, DatasourceServiceClient>();

        NodesLocator.RegisterAssembly(typeof(SqlNode).Assembly);
        NodeFormsLocator.RegisterAssembly(typeof(SqlNodeForm).Assembly);

        return services;
    }

}
