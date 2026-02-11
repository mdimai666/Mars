using System.ComponentModel;
using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.PostCategories;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.PostCategories;
using Mars.Host.Shared.Mappings.PostCategoryTypes;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Common;
using Mars.Shared.Contracts.PostCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class PostCategoryController : ControllerBase
{
    private readonly IPostCategoryService _postCategoryService;

    public PostCategoryController(IPostCategoryService postService)
    {
        _postCategoryService = postService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostCategoryDetailResponse> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _postCategoryService.GetDetail(id, cancellationToken))?.ToResponse()
                ?? throw new NotFoundException();
    }

    [HttpGet("by-type/{type}/item/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostCategoryDetailResponse> GetBySlug(string slug,
                                                    [DefaultValue("post")] string type,
                                                    bool renderContent = true,
                                                    CancellationToken cancellationToken = default)
    {
        return (await _postCategoryService.GetDetailBySlug(slug, type, cancellationToken))?.ToResponse()
                ?? throw new NotFoundException();
    }

    [HttpGet("edit/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<PostCategoryEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        return _postCategoryService.GetEditModel(id, cancellationToken);
    }

    [HttpGet("edit/blank/{categoryType}/{postType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<PostCategoryEditViewModel> GetEditModelBlank(
        [DefaultValue("default")] string categoryType, [DefaultValue("post")] string postType, CancellationToken cancellationToken)
    {
        return _postCategoryService.GetEditModelBlank(categoryType, postType, cancellationToken);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PostCategoryDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<ActionResult<PostCategoryDetailResponse>> Create([FromBody] CreatePostCategoryRequest request, CancellationToken cancellationToken)
    {
        var query = _postCategoryService.EnrichQuery(request);
        var created = await _postCategoryService.Create(query, cancellationToken);
        return Created("{id}", created.ToResponse());
    }

    [HttpPut]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostCategoryDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<PostCategoryDetailResponse> Update([FromBody] UpdatePostCategoryRequest request, CancellationToken cancellationToken)
    {
        var query = _postCategoryService.EnrichQuery(request);
        return (await _postCategoryService.Update(query, cancellationToken)).ToResponse();
    }

    [HttpGet("list/offset")]
    [AllowAnonymous]
    public async Task<ListDataResult<PostCategoryListItemResponse>> List([FromQuery] ListPostCategoryQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postCategoryService.List(request.ToQuery(null, null), cancellationToken)).ToResponse();
    }

    [HttpGet("list/page")]
    [AllowAnonymous]
    public async Task<PagingResult<PostCategoryListItemResponse>> ListTable([FromQuery] TablePostCategoryQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postCategoryService.ListTable(request.ToQuery(null, null), cancellationToken)).ToResponse();
    }

    [HttpGet("by-type/{type}/list/offset")]
    [AllowAnonymous]
    public async Task<ListDataResult<PostCategoryListItemResponse>> List([FromQuery] ListPostCategoryQueryRequest request,
                                                                    [DefaultValue("post")] string type,
                                                                    CancellationToken cancellationToken)
    {
        return (await _postCategoryService.List(request.ToQuery(postTypeName: null, categoryType: type), cancellationToken)).ToResponse();
    }

    [HttpPost("by-type/{type}/list/page")]
    [AllowAnonymous]
    public async Task<PagingResult<PostCategoryListItemResponse>> ListTable([FromQuery] TablePostCategoryQueryRequest request,
                                                                    [DefaultValue("post")] string type,
                                                                    CancellationToken cancellationToken)
    {
        return (await _postCategoryService.ListTable(request.ToQuery(postTypeName: null, categoryType: type), cancellationToken)).ToResponse();
    }

    [HttpGet("for-post-type/{postType}/list/offset")]
    [AllowAnonymous]
    public async Task<ListDataResult<PostCategoryListItemResponse>> ListForPostType([FromQuery] ListPostCategoryQueryRequest request,
                                                                    [DefaultValue("post")] string postType,
                                                                    CancellationToken cancellationToken)
    {
        return (await _postCategoryService.List(request.ToQuery(postTypeName: postType, categoryType: null), cancellationToken)).ToResponse();
    }

    [HttpPost("for-post-type/{postType}/list/page")]
    [AllowAnonymous]
    public async Task<PagingResult<PostCategoryListItemResponse>> ListForPostType([FromQuery] TablePostCategoryQueryRequest request,
                                                                    [DefaultValue("post")] string postType,
                                                                    CancellationToken cancellationToken)
    {
        return (await _postCategoryService.ListTable(request.ToQuery(postTypeName: postType, categoryType: null), cancellationToken)).ToResponse();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task Delete(Guid id, CancellationToken cancellationToken)
    {
        return _postCategoryService.Delete(id, cancellationToken);
    }

    [HttpDelete("DeleteMany")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task DeleteMany([FromQuery] Guid[] ids, CancellationToken cancellationToken)
    {
        return _postCategoryService.DeleteMany(new DeleteManyPostCategoryQuery { Ids = ids }, cancellationToken);
    }

    [HttpPost("list/by-ids")]
    [AllowAnonymous]
    public async Task<IEnumerable<PostCategoryListItemResponse>> ListByIds([FromBody] ListByIdsRequest request, CancellationToken cancellationToken)
    {
        return (await _postCategoryService.ListSummaryByIds(request.Ids, cancellationToken)).ToResponseItems();
    }
}
