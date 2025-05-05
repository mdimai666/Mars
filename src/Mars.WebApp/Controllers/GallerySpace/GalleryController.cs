using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Host.Services.GallerySpace;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers.GallerySpace;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class GalleryController : ControllerBase
{
    private readonly IGalleryService _galleryService;
    private readonly IMediaService _mediaService;
    private readonly IRequestContext _requestContext;

    public GalleryController(IGalleryService galleryService, IMediaService mediaService, IRequestContext requestContext) 
    {
        _galleryService = galleryService;
        _mediaService = mediaService;
        _requestContext = requestContext;
    }

    /*
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostDetailResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _postService.GetDetail(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet("{type}/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostDetailResponse> GetBySlug(string slug, string type, CancellationToken cancellationToken)
    {
        return (await _postService.GetDetailBySlug(slug, type, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PostDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<PostDetailResponse> Create([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
    {
        return (await _postService.Create(request.ToQuery(_requestContext.User.Id), cancellationToken)).ToResponse();
    }

    [HttpPut]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<PostDetailResponse> Update([FromBody] UpdatePostRequest request, CancellationToken cancellationToken)
    {
        return (await _postService.Update(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ListDataResult<PostListItemResponse>> List([FromQuery] ListPostQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postService.List(request.ToQuery(null), cancellationToken)).ToResponse();
    }

    [HttpPost("ListTable")]
    [AllowAnonymous]
    public async Task<PagingResult<PostListItemResponse>> ListTable([FromQuery] TablePostQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postService.ListTable(request.ToQuery(null), cancellationToken)).ToResponse();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _postService.Delete(id, cancellationToken);
        if (result.Ok) return Ok();
        return NotFound(null);
    }*/

    /*
    [HttpPost(nameof(PhotosListTable) + "/{galleryId:guid}")]
    public Task<TotalResponse<GalleryPhoto>> PhotosListTable(Guid galleryId, [FromBody] QueryFilter filter)
    {
        return modelService.GalleryPhotos(galleryId, filter);
    }

    [Authorize]
    [RequestSizeLimit(150_000_000)]
    [HttpPost(nameof(PostPhotos) + "/{galleryId:guid}")]
    public Task<UserActionResult<List<FileEntity>>> PostPhotos(Guid galleryId, [FromForm] IFormFileCollection files)
    {
        return modelService.GalleryAddPhotos(galleryId, files);
    }

    [Authorize]
    [HttpDelete(nameof(DeletePhoto) + "/{photoId:guid}")]
    public Task<UserActionResult> DeletePhoto(Guid photoId)
    {
        return modelService.GalleryDeletePhotos([photoId]);
    }
    */
}
