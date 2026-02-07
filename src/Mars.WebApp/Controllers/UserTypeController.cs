using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Host.Shared.Mappings.Search;
using Mars.Host.Shared.Mappings.UserTypes;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.UserTypes;
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
public class UserTypeController : ControllerBase
{
    private readonly IUserTypeService _userTypeService;
    public UserTypeController(
        IUserTypeService postTypeService)
    {
        _userTypeService = postTypeService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<UserTypeDetailResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _userTypeService.GetDetail(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet("edit/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<UserTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        return _userTypeService.GetEditModel(id, cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserTypeSummaryResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<ActionResult<UserTypeSummaryResponse>> Create([FromBody] CreateUserTypeRequest request, CancellationToken cancellationToken)
    {
        var created = await _userTypeService.Create(request.ToQuery(), cancellationToken);
        return Created("{id}", created.ToResponse());
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserTypeSummaryResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<UserTypeSummaryResponse> Update([FromBody] UpdateUserTypeRequest request, CancellationToken cancellationToken)
    {
        return (await _userTypeService.Update(request.ToQuery(), cancellationToken)).ToSummaryResponse();
    }

    [HttpGet("list/offset")]
    [AllowAnonymous]
    public async Task<ListDataResult<UserTypeListItemResponse>> List([FromQuery] ListUserTypeQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _userTypeService.List(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("list/page")]
    [AllowAnonymous]
    public async Task<PagingResult<UserTypeListItemResponse>> ListTable([FromQuery] TableUserTypeQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _userTypeService.ListTable(request.ToQuery(), cancellationToken)).ToResponse();
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
        return _userTypeService.Delete(id, cancellationToken);
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
        return _userTypeService.DeleteMany(new DeleteManyUserTypeQuery { Ids = ids }, cancellationToken);
    }

}
