using Mars.Datasource.Contracts.Dto;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>Реестр провайдеров: резолв по типу источника и драйверу из конфига.</summary>
public interface IDatasourceProviderRegistry
{
    IDatasourceProvider Resolve(DatasourceConfig config);

    /// <summary>Провайдер sql-источника; для другого типа источника — внятный отказ.</summary>
    ISqlDatasourceProvider ResolveSql(DatasourceConfig config);

    /// <summary>Подключённые провайдеры для формы настроек.</summary>
    IReadOnlyCollection<DatasourceDriverResponse> Describe();
}
