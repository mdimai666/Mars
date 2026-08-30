using Mars.Contracts.Common;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Dto;
using Mars.WebApiClient.Interfaces;

//namespace Mars.Datasource.Front.Services;

namespace Mars.Datasource.Front.Services;

public interface IDatasourceServiceClient
{
    Task<UserActionResult> TestConnection(ConnectionStringTestDto dto);
    Task<IReadOnlyDictionary<string, QTableColumnResponse>> Columns(string slug, string tableName);
    Task<IReadOnlyCollection<QTableSchemaResponse>> Tables(string slug);
    Task<QDatabaseStructureResponse> DatabaseStructure(string slug);
    Task<UserActionResult<string[][]>> SqlQuery(string slug, string sql);
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
