using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>Реестр провайдеров: резолв по типу источника и драйверу из конфига.</summary>
public interface IDatasourceProviderRegistry
{
    IDatasourceProvider Resolve(DatasourceConfig config);

    /// <summary>Провайдер sql-источника; для другого типа источника — внятный отказ.</summary>
    ISqlDatasourceProvider ResolveSql(DatasourceConfig config);

    /// <summary>Профили подключённых провайдеров для формы настроек.</summary>
    IReadOnlyCollection<DatasourceKindProfile> Describe();
}
