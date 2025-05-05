using Mars.Datasource.Core.Dto;
using Mars.Shared.Common;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

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

    public Task<UserActionResult<string[][]>> SqlQuery(string slug, string sql)
        => _client.Request($"{_basePath}{_controllerName}", "SqlQuery")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(new string[] { sql })
                    .ReceiveJson<UserActionResult<string[][]>>();

    public Task<UserActionResult<string[][]>> ExecuteAction(string slug, ExecuteActionRequest action)
        => _client.Request($"{_basePath}{_controllerName}", "ExecuteAction")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(action)
                    .ReceiveJson<UserActionResult<string[][]>>();

    public Task<IReadOnlyCollection<SelectDatasourceDto>> ListSelectDatasource()
        => _client.Request($"{_basePath}{_controllerName}", "ListSelectDatasource")
                    .GetJsonAsync<IReadOnlyCollection<SelectDatasourceDto>>();
}
