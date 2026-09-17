using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Datasource.Contracts.Dto;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Front.Services;

internal class DatasourceServiceClient : IDatasourceServiceClient
{
    protected readonly IFlurlClient _client;
    protected string _basePath;
    protected string _controllerName;

    public DatasourceServiceClient(IFlurlClient client)
    {
        _basePath = "/api/";
        _controllerName = "Datasource";
        _client = client;
    }

    public Task<UserActionResult> TestConnection(ConnectionStringTestDto dto)
        => _client.Request($"{_basePath}{_controllerName}", "TestConnection")
                    .PostJsonAsync(dto)
                    .ReceiveJson<UserActionResult>();

    public Task<IReadOnlyCollection<DatasourceDriverResponse>> Drivers()
        => _client.Request($"{_basePath}{_controllerName}", "Drivers")
                    .GetJsonAsync<IReadOnlyCollection<DatasourceDriverResponse>>();

    public Task<IReadOnlyDictionary<string, QTableColumnResponse>> Columns(string slug, string tableName)
        => _client.Request($"{_basePath}{_controllerName}", "Columns")
                    .AppendQueryParam(new { slug, tableName })
                    .GetJsonAsync<IReadOnlyDictionary<string, QTableColumnResponse>>();

    public Task<IReadOnlyCollection<QTableSchemaResponse>> Tables(string slug)
        => _client.Request($"{_basePath}{_controllerName}", "Tables")
                    .AppendQueryParam(new { slug })
                    .GetJsonAsync<IReadOnlyCollection<QTableSchemaResponse>>();

    public Task<QDatabaseStructureResponse> DatabaseStructure(string slug)
        => _client.Request($"{_basePath}{_controllerName}", "DatabaseStructure")
                    .AppendQueryParam(new { slug })
                    .GetJsonAsync<QDatabaseStructureResponse>();

    public Task<QDatabaseStructureResponse> RefreshStructure(string slug)
        => _client.Request($"{_basePath}{_controllerName}", "RefreshStructure")
                    .AppendQueryParam(new { slug })
                    .PostAsync()
                    .ReceiveJson<QDatabaseStructureResponse>();

    public Task<ViewDefinitionResponse> ViewDefinition(string slug, string? schema, string name)
        => _client.Request($"{_basePath}{_controllerName}", "ViewDefinition")
                    .AppendQueryParam(new { slug, schema, name })
                    .GetJsonAsync<ViewDefinitionResponse>();

    public async Task<string> Requests(string slug)
    {
        var document = await _client.Request($"{_basePath}{_controllerName}", "Requests")
                                       .AppendQueryParam(new { slug })
                                       .GetJsonAsync<RequestsDocumentDto>();

        return document.Content;
    }

    public Task<UserActionResult> SaveRequests(string slug, string content)
        => _client.Request($"{_basePath}{_controllerName}", "SaveRequests")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(new RequestsDocumentDto { Content = content ?? "" })
                    .ReceiveJson<UserActionResult>();

    public Task<DatasourceCatalog> Catalog(string slug)
        => _client.Request($"{_basePath}{_controllerName}", "Catalog")
                    .AppendQueryParam(new { slug })
                    .GetJsonAsync<DatasourceCatalog>();

    public Task<DatasourceCatalog> RefreshCatalog(string slug)
        => _client.Request($"{_basePath}{_controllerName}", "RefreshCatalog")
                    .AppendQueryParam(new { slug })
                    .GetJsonAsync<DatasourceCatalog>();

    public Task<QueryResultDto> Query(string slug, DatasourceRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "Query")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(request)
                    .ReceiveJson<QueryResultDto>();

    public Task<SqlNonQueryResultActionDto> NonQuery(string slug, DatasourceRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "NonQuery")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(request)
                    .ReceiveJson<SqlNonQueryResultActionDto>();

    public Task<UserActionResult<string[][]>> ExecuteAction(string slug, DatasourceActionRequest action)
        => _client.Request($"{_basePath}{_controllerName}", "ExecuteAction")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(action)
                    .ReceiveJson<UserActionResult<string[][]>>();

    public Task<IReadOnlyCollection<SelectDatasourceDto>> ListSelectDatasource()
        => _client.Request($"{_basePath}{_controllerName}", "ListSelectDatasource")
                    .GetJsonAsync<IReadOnlyCollection<SelectDatasourceDto>>();
}
