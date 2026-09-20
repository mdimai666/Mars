using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
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
        Profile = FileDatasourceProfile.Create();
    }

    public DatasourceKindProfile Profile { get; }

    public IDatasourceProvider Create(DatasourceConfig config) => new FileDatasourceProvider(config, _files, Profile);
}
