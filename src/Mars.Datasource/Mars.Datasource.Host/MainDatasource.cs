using System.Text;
using Mars.CommandLine.Abstractions;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Abstractions.Services;
using Mars.Datasource.Host.CommandLine;
using Mars.Datasource.Host.Nodes;
using Mars.Datasource.Host.Services;
using Mars.Datasource.Nodes;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Options.Abstractions.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Host;

public static class MainDatasource
{
    public static IServiceCollection AddDatasourceHost(this IServiceCollection services)
    {
        services.AddSingleton<IDatasourceService, DatasourceService>();
        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();
        services.AddScoped<IDatasourceAIToolSchemaProviderHandler, DatasourceAIToolSchemaProviderHandler>();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _ = nameof(System.Text.Encoding.CodePage); //Используется в sql запросах.

        return services;
    }

    public static IApplicationBuilder UseDatasourceHost(this IApplicationBuilder app)
    {

        IOptionService optionService = app.ApplicationServices.GetRequiredService<IOptionService>()!;
        optionService.RegisterOption<DatasourceOption>();

        app.ApplicationServices.GetRequiredService<INodeImplementFactory>().RegisterAssembly(typeof(SqlNodeImpl).Assembly);
        app.ApplicationServices.GetRequiredService<INodesLocator>().RegisterAssembly(typeof(SqlNode).Assembly);

        app.ApplicationServices.GetRequiredService<ICommandLineApi>().Register<DataSourceCli>();

        return app;
    }

}
