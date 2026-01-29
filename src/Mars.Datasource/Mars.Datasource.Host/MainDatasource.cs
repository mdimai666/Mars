using System.Text;
using Mars.Datasource.Core;
using Mars.Datasource.Core.Nodes;
using Mars.Datasource.Host.CommandLine;
using Mars.Datasource.Host.Nodes;
using Mars.Datasource.Host.Services;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Host;

public static class DatasourceHostExtensions
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

        app.ApplicationServices.GetRequiredService<NodeImplementFactory>().RegisterAssembly(typeof(SqlNodeImpl).Assembly);
        app.ApplicationServices.GetRequiredService<NodesLocator>().RegisterAssembly(typeof(SqlNode).Assembly);

        app.ApplicationServices.GetRequiredService<ICommandLineApi>().Register<DataSourceCli>();

        return app;
    }

}
