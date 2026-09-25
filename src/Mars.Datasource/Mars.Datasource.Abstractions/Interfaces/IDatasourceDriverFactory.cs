using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>
/// Провайдер источника: чем создать драйвер и что показать в форме настроек.
/// Реализация живёт в проекте провайдера и подключается его Add-хуком в корне композиции,
/// поэтому ядро модуля не знает конкретных движков.
/// </summary>
public interface IDatasourceDriverFactory
{
    /// <summary>Ключ движка (`psql` | `mssql` | `mysql`), он же значение <see cref="DatasourceConfig.Driver"/>.</summary>
    public string Driver { get; }

    /// <summary>Подпись движка в форме настроек (`PostgreSQL`).</summary>
    public string DisplayName { get; }

    /// <summary>Строка подключения для новой настройки — подсказка в форме редактора.</summary>
    public string DefaultConnectionString { get; }

    /// <summary>Документация по строке подключения — ссылка в форме редактора.</summary>
    public string HelpLink { get; }

    public IDatasourceDriver Create(DatasourceConfig config);
}
