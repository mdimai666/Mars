using System.Net.Mime;
using Mars.Cms.Abstractions.Dto.NavMenus;
using Mars.Cms.Abstractions.Mappings.NavMenus;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.NavMenus;
using Mars.Contracts.Common;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Cms.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class NavMenuController : ControllerBase
{
    private readonly INavMenuService _navMenuService;

    public NavMenuController(INavMenuService navMenuService)
    {
        _navMenuService = navMenuService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<NavMenuDetailResponse?> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _navMenuService.GetDetail(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    //[HttpGet("/edit/{id:guid}")]
    //public Task<NavMenuEditModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    //{
    //    return _navMenuService.GetEditModel(id);
    //}

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateNavMenuRequest request, CancellationToken cancellationToken)
    {
        var created = await _navMenuService.Create(request.ToQuery(), cancellationToken);
        return Created("{id}", created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task Update(Guid id, [FromBody] UpdateNavMenuRequest request, CancellationToken cancellationToken)
    {
        await _navMenuService.Update(request.ToQuery(), cancellationToken);
    }

    [HttpGet("list/offset")]
    public async Task<ListDataResult<NavMenuSummaryResponse>> List([FromQuery] ListNavMenuQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _navMenuService.List(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("list/page")]
    public async Task<PagingResult<NavMenuSummaryResponse>> ListTable([FromQuery] TableNavMenuQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _navMenuService.ListTable(request.ToQuery(), cancellationToken)).ToResponse();
    }

    /// <summary>
    /// Список меню для админки: несохранённые системные меню подмешиваются виртуальной записью.
    /// </summary>
    [HttpGet("admin/list/offset")]
    public async Task<ListDataResult<NavMenuSummaryResponse>> ListForAdmin([FromQuery] ListNavMenuQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _navMenuService.ListForAdmin(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task Delete(Guid id, CancellationToken cancellationToken)
    {
        return _navMenuService.Delete(id, cancellationToken);
    }

    [HttpDelete("DeleteMany")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task DeleteMany([FromQuery] Guid[] ids, CancellationToken cancellationToken)
    {
        return _navMenuService.DeleteMany(new DeleteManyNavMenuQuery { Ids = ids }, cancellationToken);
    }

    /// <summary>
    /// Сброс системного меню к дефолту: удаляет сохранённую копию из БД.
    /// </summary>
    [HttpPost("{id:guid}/reset")]
    public async Task<UserActionResult> Reset(Guid id, CancellationToken cancellationToken)
    {
        await _navMenuService.Reset(id, cancellationToken);
        return UserActionResult.Success("Меню сброшено к дефолтному состоянию");
    }

    //---------------

    [Authorize(Roles = "Admin")]
    [HttpGet("Export/{id:guid}")]
    public Task<NavMenuExport> Export(Guid id)
    {
        return _navMenuService.Export(id);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("Import/{id:guid}")]
    public Task<UserActionResult> Import(Guid id, NavMenuImport navmenu)
    {
        return _navMenuService.Import(id, navmenu);
    }

    //[Authorize(Roles = "Admin")]
    //[HttpPost(nameof(ImportFile))]
    //public async Task<UserActionResult> ImportFile(IFormFile file)
    //{
    //    using var reader = new StreamReader(file.OpenReadStream());

    //    string json = reader.ReadToEnd();

    //    NavMenu navmenu = JsonConvert.DeserializeObject<NavMenu>(json);

    //    return await modelService.Import(navmenu);
    //}
}
