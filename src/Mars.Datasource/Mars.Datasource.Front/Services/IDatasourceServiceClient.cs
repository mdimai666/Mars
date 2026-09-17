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

    /// <summary>Каталог объектов источника — дерево для любого типа источника.</summary>
    Task<DatasourceCatalog> Catalog(string slug);

    /// <summary>Перечитать каталог источника, минуя серверный кэш.</summary>
    Task<DatasourceCatalog> RefreshCatalog(string slug);

    /// <summary>Определение вьюхи; пустой `Sql` — движок текст не отдал.</summary>
    Task<ViewDefinitionResponse> ViewDefinition(string slug, string? schema, string name);

    /// <summary>Документ запросов источника (rest): `.http` с запросами пользователя.</summary>
    Task<string> Requests(string slug);

    /// <summary>Сохранить документ запросов источника.</summary>
    Task<UserActionResult> SaveRequests(string slug, string content);
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
