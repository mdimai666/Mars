using System.ComponentModel;
using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Host.Shared.Mappings.PostCategoryTypes;
using Mars.Host.Shared.Mappings.Search;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategoryTypes;
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
public class PostCategoryTypeController : ControllerBase
{
    private readonly IPostCategoryTypeService _postCategoryTypeService;
    public PostCategoryTypeController(
        IPostCategoryTypeService postTypeService)
    {
        _postCategoryTypeService = postTypeService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostCategoryTypeDetailResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _postCategoryTypeService.GetDetail(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet("by-name/{typeName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostCategoryTypeDetailResponse> GetByName([DefaultValue("default")] string typeName, CancellationToken cancellationToken)
    {
        return (await _postCategoryTypeService.GetDetailByName(typeName, cancellationToken))?.ToResponse()
                ?? throw new NotFoundException();
    }

    [HttpGet("edit/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<PostCategoryTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        return _postCategoryTypeService.GetEditModel(id, cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PostCategoryTypeSummaryResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<ActionResult<PostCategoryTypeSummaryResponse>> Create([FromBody] CreatePostCategoryTypeRequest request, CancellationToken cancellationToken)
    {
        var created = await _postCategoryTypeService.Create(request.ToQuery(), cancellationToken);
        return Created("{id}", created.ToResponse());
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostCategoryTypeSummaryResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<PostCategoryTypeSummaryResponse> Update([FromBody] UpdatePostCategoryTypeRequest request, CancellationToken cancellationToken)
    {
        return (await _postCategoryTypeService.Update(request.ToQuery(), cancellationToken)).ToSummaryResponse();
    }

    [HttpGet("list/offset")]
    [AllowAnonymous]
    public async Task<ListDataResult<PostCategoryTypeListItemResponse>> List([FromQuery] ListPostCategoryTypeQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postCategoryTypeService.List(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("list/page")]
    [AllowAnonymous]
    public async Task<PagingResult<PostCategoryTypeListItemResponse>> ListTable([FromQuery] TablePostCategoryTypeQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postCategoryTypeService.ListTable(request.ToQuery(), cancellationToken)).ToResponse();
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
        return _postCategoryTypeService.Delete(id, cancellationToken);
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
        return _postCategoryTypeService.DeleteMany(new DeleteManyPostCategoryTypeQuery { Ids = ids }, cancellationToken);
    }

}
