using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;

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

    public Task<IReadOnlyCollection<DatasourceKindProfile>> Providers()
        => _client.Request($"{_basePath}{_controllerName}", "Providers")
                    .GetJsonAsync<IReadOnlyCollection<DatasourceKindProfile>>();

    public Task<ViewDefinitionResponse> ViewDefinition(string slug, string? schema, string name)
        => _client.Request($"{_basePath}{_controllerName}", "ViewDefinition")
                    .AppendQueryParam(new { slug, schema, name })
                    .GetJsonAsync<ViewDefinitionResponse>();

    public async Task<string> Document(string slug, string name)
    {
        var document = await _client.Request($"{_basePath}{_controllerName}", "Document")
                                       .AppendQueryParam(new { slug, name })
                                       .GetJsonAsync<DocumentDto>();

        return document.Content;
    }

    public Task<UserActionResult> SaveDocument(string slug, string name, string content)
        => _client.Request($"{_basePath}{_controllerName}", "SaveDocument")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(new DocumentDto { Name = name, Content = content ?? "" })
                    .ReceiveJson<UserActionResult>();

    public Task<DatasourceCatalog> Catalog(string slug, bool refresh = false)
        => _client.Request($"{_basePath}{_controllerName}", "Catalog")
                    .AppendQueryParam(new { slug, refresh })
                    .GetJsonAsync<DatasourceCatalog>();

    public Task<QueryResultDto> Query(string slug, DatasourceRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "Query")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(request)
                    .ReceiveJson<QueryResultDto>();

    public Task<DatasourceModifyResult> Modify(string slug, DatasourceRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "Modify")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(request)
                    .ReceiveJson<DatasourceModifyResult>();

    public Task<UserActionResult<string[][]>> ExecuteAction(string slug, DatasourceActionRequest action)
        => _client.Request($"{_basePath}{_controllerName}", "ExecuteAction")
                    .AppendQueryParam(new { slug })
                    .PostJsonAsync(action)
                    .ReceiveJson<UserActionResult<string[][]>>();

    public Task<IReadOnlyCollection<SelectDatasourceDto>> ListSelectDatasource()
        => _client.Request($"{_basePath}{_controllerName}", "ListSelectDatasource")
                    .GetJsonAsync<IReadOnlyCollection<SelectDatasourceDto>>();
}
