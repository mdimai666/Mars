using Mars.Datasource.Core.Dto;
using Mars.Datasource.Front.Services;
using Mars.Shared.Common;

//namespace Mars.Datasource.Front.Services;
namespace Mars.WebApiClient.Interfaces;

public interface IDatasourceServiceClient
{
    Task<UserActionResult> TestConnection(ConnectionStringTestDto dto);
    Task<IReadOnlyDictionary<string, QTableColumnResponse>> Columns(string slug, string tableName);
    Task<IReadOnlyCollection<QTableSchemaResponse>> Tables(string slug);
    Task<QDatabaseStructureResponse> DatabaseStructure(string slug);
    Task<UserActionResult<string[][]>> SqlQuery(string slug, string sql);
    Task<UserActionResult<string[][]>> ExecuteAction(string slug, ExecuteActionRequest action);
    Task<IReadOnlyCollection<SelectDatasourceDto>> ListSelectDatasource();

}

public static class WebApiClientDatasourceClientExtensions
{
    public static IDatasourceServiceClient Datasource(this IMarsWebApiClient client)
    {
        return new DatasourceServiceClient(client.Client);
    }
}
