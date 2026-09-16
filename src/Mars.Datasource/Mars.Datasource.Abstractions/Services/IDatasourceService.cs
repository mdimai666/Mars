using Mars.Contracts.Common;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Contracts.Dto;

namespace Mars.Datasource.Abstractions.Services;

public interface IDatasourceService
{
    public DatasourceConfig DefaultConfig { get; }

    /// <summary>Зарегистрированные провайдеры: тип источника, ключ драйвера, подсказка строки подключения, ссылка на док.</summary>
    public IReadOnlyCollection<DatasourceDriverResponse> Drivers();

    public void InvalidateLocalDictCache(DatasourceOption opt);
    public Task<UserActionResult> TestConnection(ConnectionStringTestDto dto);
    public Task<Dictionary<string, QTableColumn>> Columns(string slug, string tableName);
    public Task<List<QTableSchema>> Tables(string slug);
    public Task<QDatabaseStructure> DatabaseStructure(string slug);

    /// <summary>Каталог объектов источника: общий вид дерева для любого типа источника.</summary>
    public Task<DatasourceCatalog> Catalog(string slug);

    /// <summary>Перечитать каталог источника, минуя кэш.</summary>
    public Task<DatasourceCatalog> RefreshCatalog(string slug);

    /// <summary>Определение вьюхи (текст запроса), null — объекта нет или это не вьюха.</summary>
    public Task<string?> ViewDefinition(string slug, string? schemaName, string tableName);

    /// <summary>Перечитать структуру базы, минуя кэш.</summary>
    public Task<QDatabaseStructure> RefreshStructure(string slug);

    /// <summary>Выполнить запрос с возвратом данных.</summary>
    public Task<QueryResultDto> Query(string slug, DatasourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Выполнить запрос без возврата данных (INSERT/UPDATE/DELETE/DDL).</summary>
    public Task<SqlNonQueryResultActionDto> NonQuery(string slug, string sql, IReadOnlyList<DatasourceParam>? parameters = null, CancellationToken cancellationToken = default);

    public Task<UserActionResult<string[][]>> ExecuteAction(string slug, DatasourceActionRequest action, CancellationToken cancellationToken);
    public IEnumerable<SelectDatasourceDto> ListSelectDatasource();

}
