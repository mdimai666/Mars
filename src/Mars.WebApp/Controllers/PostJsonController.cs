using System.ComponentModel;
using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.PostJsons;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Mappings.PostJsons;
using Mars.Host.Shared.Mappings.Posts;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostJsons;
using Mars.Shared.Contracts.Posts;
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
public class PostJsonController : ControllerBase
{
    private readonly IPostJsonService _postJsonService;
    private readonly IRequestContext _requestContext;

    public PostJsonController(IPostJsonService postJsonService, IRequestContext requestContext)
    {
        _postJsonService = postJsonService;
        _requestContext = requestContext;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostJsonResponse> Get(Guid id,
                                                bool renderContent = true,
                                                CancellationToken cancellationToken = default)
    {
        return (await _postJsonService.GetDetail(id, renderContent: renderContent, cancellationToken))?.ToResponse()
                            ?? throw new NotFoundException();
    }

    [HttpGet("by-type/{type}/item/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostJsonResponse> GetBySlug(string slug,
                                                    string type,
                                                    bool renderContent = true,
                                                    CancellationToken cancellationToken = default)
    {
        return (await _postJsonService.GetDetailBySlug(slug, type, renderContent: renderContent, cancellationToken))?.ToResponse()
                            ?? throw new NotFoundException();
    }

    [HttpGet("by-type/{type}/list/offset")]
    public async Task<ListDataResult<PostJsonResponse>> List([FromQuery] ListPostQueryRequest request, [DefaultValue("post")] string type, CancellationToken cancellationToken)
    {
        return (await _postJsonService.List(request.ToQuery(type), cancellationToken)).ToResponse();
    }

    [HttpGet("by-type/{type}/list/page")]
    public async Task<PagingResult<PostJsonResponse>> ListTable([FromQuery] TablePostQueryRequest request, [DefaultValue("post")] string type, CancellationToken cancellationToken)
    {
        return (await _postJsonService.ListTable(request.ToQuery(type), cancellationToken)).ToResponse();
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<ActionResult<PostJsonResponse>> Create([FromBody] CreatePostJsonRequest request, CancellationToken cancellationToken)
    {
        var created = await _postJsonService.Create(request.ToQuery(_requestContext.User.Id), cancellationToken);
        return Created("{id}", created.ToResponse());
    }

    [HttpPut]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<PostJsonResponse> Update([FromBody] UpdatePostJsonRequest request, CancellationToken cancellationToken)
    {
        return (await _postJsonService.Update(request.ToQuery(_requestContext.User.Id), cancellationToken)).ToResponse();
    }
}
