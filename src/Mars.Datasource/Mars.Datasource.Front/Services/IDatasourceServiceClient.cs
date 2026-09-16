using Mars.Contracts.Common;
using Mars.Datasource.Contracts.Dto;
using Mars.Datasource.Contracts.Models;
using Mars.WebApiClient.Interfaces;

//namespace Mars.Datasource.Front.Services;

namespace Mars.Datasource.Front.Services;

public interface IDatasourceServiceClient
{
    Task<UserActionResult> TestConnection(ConnectionStringTestDto dto);

    /// <summary>Подключённые провайдеры источников (список движков для формы настроек).</summary>
    Task<IReadOnlyCollection<DatasourceDriverResponse>> Drivers();
    Task<IReadOnlyDictionary<string, QTableColumnResponse>> Columns(string slug, string tableName);
    Task<IReadOnlyCollection<QTableSchemaResponse>> Tables(string slug);
    Task<QDatabaseStructureResponse> DatabaseStructure(string slug);

    /// <summary>Каталог объектов источника — дерево для любого типа источника.</summary>
    Task<DatasourceCatalog> Catalog(string slug);

    /// <summary>Перечитать структуру базы, минуя серверный кэш.</summary>
    Task<QDatabaseStructureResponse> RefreshStructure(string slug);

    /// <summary>Определение вьюхи; пустой `Sql` — движок текст не отдал.</summary>
    Task<ViewDefinitionResponse> ViewDefinition(string slug, string? schema, string name);
    Task<QueryResultDto> Query(string slug, DatasourceRequest request);
    Task<SqlNonQueryResultActionDto> NonQuery(string slug, DatasourceRequest request);
    Task<UserActionResult<string[][]>> ExecuteAction(string slug, DatasourceActionRequest action);
    Task<IReadOnlyCollection<SelectDatasourceDto>> ListSelectDatasource();

}

public static class WebApiClientDatasourceClientExtensions
{
    public static IDatasourceServiceClient Datasource(this IMarsWebApiClient client)
    {
        return new DatasourceServiceClient(client.Client);
    }
}
