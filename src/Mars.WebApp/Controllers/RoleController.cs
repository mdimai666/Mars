using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Roles;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.Roles;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<RoleDetailResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _roleService.Get(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    //[HttpGet("/edit/{id:guid}")]
    //public Task<RoleEditModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    //{
    //    return _roleService.GetEditModel(id);
    //}

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(RoleDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<ActionResult<RoleDetailResponse>> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var created = await _roleService.Create(request.ToQuery(), cancellationToken);
        return Created("{id}", created.ToResponse());
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RoleDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<RoleDetailResponse> Update([FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        return (await _roleService.Update(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet]
    public async Task<ListDataResult<RoleSummaryResponse>> List([FromQuery] ListRoleQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _roleService.List(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("ListTable")]
    public async Task<PagingResult<RoleSummaryResponse>> ListTable([FromQuery] TableRoleQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _roleService.ListTable(request.ToQuery(), cancellationToken)).ToResponse();
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
        return _roleService.Delete(id, cancellationToken);
    }

    //--------------------

    //EditRolesViewModelDto

    //[HttpGet(nameof(EditRolesViewModel))]
    //public async Task<ActionResult<EditRolesViewModelDto>> EditRolesViewModel()
    //{
    //    return await _roleService.EditRolesViewModel();
    //}

    //[HttpPut(nameof(SaveRoleClaims))]
    //public async Task<UserActionResult> SaveRoleClaims(EditRolesViewModelDto dto)
    //{
    //    return await _roleService.SaveRoleClaims(dto);
    //}

    //#if DEBUG
    //    [HttpGet(nameof(TestClaim))]
    //    //[claim]
    //    public async Task<UserActionResult> TestClaim()
    //    {
    //        var result = _roleService.HasCap(RoleCaps.PostCap.Add).Result;

    //        return new UserActionResult
    //        {
    //            Ok = result,
    //            Message = result ? "Да" : "Нет"
    //        };
    //    }
    //#endif
}
