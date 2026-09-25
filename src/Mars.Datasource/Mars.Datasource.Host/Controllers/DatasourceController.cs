using System.ComponentModel;
using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Datasource.Abstractions.Services;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
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
    private readonly IDatasourceRegistry registry;
    private readonly IDatasourceService ds;
    private readonly ISqlDatasourceService sql;

    public DatasourceController(IDatasourceRegistry registry, IDatasourceService ds, ISqlDatasourceService sql)
    {
        this.registry = registry;
        this.ds = ds;
        this.sql = sql;
    }

    [HttpPost]
    public Task<UserActionResult> TestConnection(ConnectionStringTestDto dto)
    {
        return registry.TestConnection(dto);
    }

    /// <summary>Каталог объектов источника: дерево и действия для любого типа источника, не только для баз данных.</summary>
    [HttpGet]
    public async Task<DatasourceCatalog> Catalog(string slug, bool refresh = false)
    {
        return refresh ? await ds.RefreshCatalog(slug) : await ds.Catalog(slug);
    }

    /// <summary>Документ запросов источника (`.http` у rest): имя документа объявляет провайдер.</summary>
    [HttpGet]
    public async Task<DocumentDto> Document([DefaultValue("default")] string slug, string name)
    {
        return new DocumentDto { Name = name, Content = await ds.Document(slug, name) };
    }

    /// <summary>Сохранить документ запросов источника.</summary>
    [HttpPost]
    public Task<UserActionResult> SaveDocument([FromQuery] string slug, [FromBody] DocumentDto document)
    {
        return ds.SaveDocument(slug, document.Name, document.Content);
    }

    [HttpGet]
    public async Task<ViewDefinitionResponse> ViewDefinition([DefaultValue("default")] string slug, string? schema, string name)
    {
        return new ViewDefinitionResponse { Sql = await sql.ViewDefinition(slug, schema, name) ?? "" };
    }

    [HttpPost]
    public Task<QueryResultDto> Query([FromQuery] string slug, [FromBody] DatasourceRequest request, CancellationToken cancellationToken)
    {
        return ds.Query(slug, request, cancellationToken);
    }

    [HttpPost]
    public Task<DatasourceModifyResult> Modify([FromQuery] string slug, [FromBody] DatasourceRequest request, CancellationToken cancellationToken)
    {
        return ds.Modify(slug, request, cancellationToken);
    }

    [HttpPost]
    public Task<UserActionResult<string[][]>> ExecuteAction([DefaultValue("default")] string slug, DatasourceActionRequest action, CancellationToken cancellationToken)
    {
        return ds.ExecuteAction(slug, action, cancellationToken);
    }

    [HttpGet]
    public ActionResult<IEnumerable<SelectDatasourceDto>> ListSelectDatasource()
    {
        return Ok(registry.ListSelectDatasource());
    }

    /// <summary>Профили подключённых провайдеров: форма настроек рисует по ним типы источника и их поля.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<DatasourceKindProfile>> Providers()
    {
        return Ok(registry.Providers());
    }
}
