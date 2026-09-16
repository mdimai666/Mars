using System.ComponentModel;
using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Abstractions.Services;
using Mars.Datasource.Contracts.Dto;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Contracts.Models;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Datasource.Host.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class DatasourceController : ControllerBase
{

    private readonly IDatasourceService ds;

    public DatasourceController(IDatasourceService ds)
    {
        this.ds = ds;
    }

    [HttpPost]
    public Task<UserActionResult> TestConnection(ConnectionStringTestDto dto)
    {
        return ds.TestConnection(dto);
    }

    [HttpGet]
    public async Task<IReadOnlyDictionary<string, QTableColumnResponse>> Columns(string slug, string tableName)
    {
        return (await ds.Columns(slug, tableName)).ToResponse();
    }

    [HttpGet]
    public async Task<IReadOnlyCollection<QTableSchemaResponse>> Tables(string slug)
    {
        return (await ds.Tables(slug)).ToResponse();
    }

    [HttpGet]
    public async Task<ActionResult<QDatabaseStructureResponse>> DatabaseStructure(string slug)
    {
        return (await ds.DatabaseStructure(slug)).ToResponse();
    }

    /// <summary>Каталог объектов источника: дерево для любого типа источника, не только для баз данных.</summary>
    [HttpGet]
    public async Task<DatasourceCatalog> Catalog(string slug)
    {
        return await ds.Catalog(slug);
    }

    /// <summary>Перечитать каталог, минуя серверный кэш (кнопка «обновить» в дереве).</summary>
    [HttpGet]
    public async Task<DatasourceCatalog> RefreshCatalog(string slug)
    {
        return await ds.RefreshCatalog(slug);
    }

    [HttpGet]
    public async Task<ViewDefinitionResponse> ViewDefinition([DefaultValue("default")] string slug, string? schema, string name)
    {
        return new ViewDefinitionResponse { Sql = await ds.ViewDefinition(slug, schema, name) ?? "" };
    }

    [HttpPost]
    public async Task<ActionResult<QDatabaseStructureResponse>> RefreshStructure(string slug)
    {
        return (await ds.RefreshStructure(slug)).ToResponse();
    }

    [HttpPost]
    public Task<QueryResultDto> Query([FromQuery] string slug, [FromBody] DatasourceRequest request, CancellationToken cancellationToken)
    {
        return ds.Query(slug, request, cancellationToken);
    }

    [HttpPost]
    public Task<SqlNonQueryResultActionDto> NonQuery([FromQuery] string slug, [FromBody] DatasourceRequest request, CancellationToken cancellationToken)
    {
        return ds.NonQuery(slug, request.Query, request.Parameters, cancellationToken);
    }

    [HttpPost]
    public Task<UserActionResult<string[][]>> ExecuteAction([DefaultValue("default")] string slug, DatasourceActionRequest action, CancellationToken cancellationToken)
    {
        return ds.ExecuteAction(slug, action, cancellationToken);
    }

    [HttpGet]
    public ActionResult<IEnumerable<SelectDatasourceDto>> ListSelectDatasource()
    {
        return Ok(ds.ListSelectDatasource());
    }

    /// <summary>Провайдеры, подключённые к модулю: ключ, подсказка строки подключения, ссылка на док.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<DatasourceDriverResponse>> Drivers()
    {
        return Ok(ds.Drivers());
    }
}
