using Mars.Contracts.Common;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.WebApiClient.Interfaces;

namespace Mars.Datasource.Front.Services;

public interface IDatasourceServiceClient
{
    Task<UserActionResult> TestConnection(ConnectionStringTestDto dto);

    /// <summary>Профили подключённых провайдеров: типы источника и их поля настроек.</summary>
    Task<IReadOnlyCollection<DatasourceKindProfile>> Providers();

    /// <summary>Каталог источника: дерево объектов, профиль и действия; <paramref name="refresh"/> — минуя серверный кэш.</summary>
    Task<DatasourceCatalog> Catalog(string slug, bool refresh = false);

    /// <summary>Определение вьюхи; пустой `Sql` — движок текст не отдал.</summary>
    Task<ViewDefinitionResponse> ViewDefinition(string slug, string? schema, string name);

    /// <summary>Документ запросов источника (`.http` у rest).</summary>
    Task<string> Document(string slug, string name);

    /// <summary>Сохранить документ запросов источника.</summary>
    Task<UserActionResult> SaveDocument(string slug, string name, string content);

    Task<QueryResultDto> Query(string slug, DatasourceRequest request);

    /// <summary>Запрос, который меняет источник (INSERT/UPDATE/DDL, POST/PUT/DELETE).</summary>
    Task<DatasourceModifyResult> Modify(string slug, DatasourceRequest request);

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
