using System.Net.Mime;
using Mars.Datasource.Core;
using Mars.Datasource.Core.Dto;
using Mars.Datasource.Core.Mappings;
using Mars.Datasource.Host.Services;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Shared.Common;
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

    [HttpPost]
    public Task<SqlQueryResultActionDto> SqlQuery([FromQuery] string slug, [FromBody] string[] _sql)
    {
        return ds.SqlQuery(slug, _sql[0]);
    }

    [HttpPost]
    public Task<UserActionResult<string[][]>> ExecuteAction(string slug, ExecuteActionRequest action)
    {
        return ds.ExecuteAction(action);
    }

    [HttpGet]
    public ActionResult<IEnumerable<SelectDatasourceDto>> ListSelectDatasource()
    {
        return Ok(ds.ListSelectDatasource());
    }
}



