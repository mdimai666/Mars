using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Providers.File;

/// <summary>
/// Подключение файлового провайдера. Ядро модуля провайдеров не знает — их приносит
/// корень композиции (см. <c>AddDatasource</c> в проекте Mars.Datasource).
/// </summary>
public static class MainDatasourceProvider
{
    public static IServiceCollection AddDatasourceFile(this IServiceCollection services)
    {
        services.AddSingleton<IDatasourceProviderFactory, FileDatasourceProviderFactory>();

        return services;
    }
}

class FileDatasourceProviderFactory : IDatasourceProviderFactory
{
    readonly IDatasourceFileSource _files;

    public FileDatasourceProviderFactory(IDatasourceFileSource files)
    {
        _files = files;
    }

    public string Kind => DatasourceKind.File;

    public string Driver => "";

    public string DisplayName => "csv / xlsx";

    public string DefaultConnectionString => "";

    public string HelpLink => "https://mdimai666.github.io/Mars/";

    public IDatasourceProvider Create(DatasourceConfig config) => new FileDatasourceProvider(config, _files);
}
