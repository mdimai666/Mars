using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// Подключение rest-провайдера. Ядро модуля провайдеров не знает — их приносит
/// корень композиции (см. <c>AddDatasource</c> в проекте Mars.Datasource).
/// </summary>
public static class MainDatasourceProvider
{
    public static IServiceCollection AddDatasourceRest(this IServiceCollection services)
    {
        services.AddSingleton<RestHttpClientCache>();
        services.AddSingleton<IRestCatalogDiscovery, WordPressRestDiscovery>();
        services.AddSingleton<IRestCatalogDiscovery, OpenApiRestDiscovery>();
        services.AddSingleton<IDatasourceProviderFactory, RestDatasourceProviderFactory>();

        return services;
    }
}

class RestDatasourceProviderFactory : IDatasourceProviderFactory
{
    readonly IDatasourceStore _store;
    readonly RestHttpClientCache _clients;
    readonly IEnumerable<IRestCatalogDiscovery> _discoveries;

    public RestDatasourceProviderFactory(IDatasourceStore store, RestHttpClientCache clients,
        IEnumerable<IRestCatalogDiscovery> discoveries)
    {
        _store = store;
        _clients = clients;
        _discoveries = discoveries;
        Profile = RestDatasourceProfile.Create();
    }

    public DatasourceKindProfile Profile { get; }

    public IDatasourceProvider Create(DatasourceConfig config)
        => new RestDatasourceProvider(config, _store, _clients, _discoveries, Profile);
}
