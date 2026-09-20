using System.Text;
using Mars.CommandLine.Abstractions;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Abstractions.Services;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Nodes;
using Mars.Datasource.Host.CommandLine;
using Mars.Datasource.Host.Nodes;
using Mars.Datasource.Host.Services;
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
        services.AddSingleton<IDatasourceProviderRegistry, DatasourceProviderRegistry>();
        services.AddSingleton<IDatasourceFileSource, DatasourceFileSource>();
        services.AddSingleton<IDatasourceStore, DatasourceStore>();

        // Один singleton на три грани сервиса: реестр источников, запросы и sql-специфика.
        services.AddSingleton<DatasourceService>();
        services.AddSingleton<IDatasourceRegistry>(provider => provider.GetRequiredService<DatasourceService>());
        services.AddSingleton<IDatasourceService>(provider => provider.GetRequiredService<DatasourceService>());
        services.AddSingleton<ISqlDatasourceService>(provider => provider.GetRequiredService<DatasourceService>());

        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _ = nameof(System.Text.Encoding.CodePage); //Используется в sql запросах.

        return services;
    }

    public static IApplicationBuilder UseDatasourceHost(this IApplicationBuilder app)
    {

        IOptionService optionService = app.ApplicationServices.GetRequiredService<IOptionService>()!;
        IDatasourceRegistry registry = app.ApplicationServices.GetRequiredService<IDatasourceRegistry>();
        optionService.RegisterOption<DatasourceOption>(registry.InvalidateLocalDictCache);

        app.ApplicationServices.GetRequiredService<INodeImplementFactory>().RegisterAssembly(typeof(SqlNodeImpl).Assembly);
        app.ApplicationServices.GetRequiredService<INodesLocator>().RegisterAssembly(typeof(SqlNode).Assembly);

        app.ApplicationServices.GetRequiredService<ICommandLineApi>().Register<DataSourceCli>();

        return app;
    }

}
