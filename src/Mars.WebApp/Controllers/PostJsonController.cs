using System.Net.Mime;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.Posts;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;
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

    public PostJsonController(IPostJsonService postJsonService)
    {
        _postJsonService = postJsonService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostJsonResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _postJsonService.GetDetail(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet("{type}/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostJsonResponse> GetBySlug(string slug, string type, CancellationToken cancellationToken)
    {
        return (await _postJsonService.GetDetailBySlug(slug, type, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet("{type}")]
    public async Task<ListDataResult<PostJsonResponse>> List([FromQuery] ListPostQueryRequest request, string type, CancellationToken cancellationToken)
    {
        return (await _postJsonService.List(request.ToQuery(type), cancellationToken)).ToResponse();
    }

    [HttpGet("ListTable/{type}")]
    public async Task<PagingResult<PostJsonResponse>> ListTable([FromQuery] TablePostQueryRequest request, string type, CancellationToken cancellationToken)
    {
        return (await _postJsonService.ListTable(request.ToQuery(type), cancellationToken)).ToResponse();
    }
}
