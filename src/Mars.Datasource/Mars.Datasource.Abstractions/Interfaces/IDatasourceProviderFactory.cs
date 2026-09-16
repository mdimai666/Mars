using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>
/// Чем создать провайдер источника и что показать о нём в форме настроек.
/// Реализация живёт в проекте провайдера и подключается его Add-хуком, поэтому ядро модуля
/// не знает конкретных источников. sql-драйверы (<see cref="IDatasourceDriverFactory"/>)
/// оборачиваются в эту фабрику реестром.
/// </summary>
public interface IDatasourceProviderFactory
{
    /// <summary>Тип источника: значение <see cref="DatasourceConfig.Kind"/>.</summary>
    public string Kind { get; }

    /// <summary>Вариант провайдера внутри типа: у sql — движок (`psql`|`mssql`|`mysql`), пусто, если вариант один.</summary>
    public string Driver { get; }

    public string DisplayName { get; }

    /// <summary>Подсказка строки подключения для новой настройки; пустая у источников без строки подключения.</summary>
    public string DefaultConnectionString { get; }

    public string HelpLink { get; }

    public IDatasourceProvider Create(DatasourceConfig config);
}
