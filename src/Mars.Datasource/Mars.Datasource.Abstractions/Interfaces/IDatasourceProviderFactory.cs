using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>
/// Чем создать провайдер источника и что показать о нём в форме настроек.
/// Реализация живёт в проекте провайдера и подключается его Add-хуком, поэтому ядро модуля
/// не знает конкретных источников. sql-драйверы (<see cref="IDatasourceDriverFactory"/>)
/// оборачиваются в эту фабрику реестром.
/// </summary>
public interface IDatasourceProviderFactory
{
    /// <summary>
    /// Профиль типа источника: <see cref="DatasourceKindProfile.Kind"/> и
    /// <see cref="DatasourceKindProfile.Driver"/> участвуют в резолве провайдера,
    /// остальное читает форма настроек.
    /// </summary>
    public DatasourceKindProfile Profile { get; }

    public IDatasourceProvider Create(DatasourceConfig config);
}
