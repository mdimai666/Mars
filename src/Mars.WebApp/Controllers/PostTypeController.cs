using System.ComponentModel.DataAnnotations;
using Mars.Server.Abstractions.ExceptionFilters;
using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Mappings.MetaFields;
using Mars.Cms.Abstractions.Mappings.PostTypes;
using Mars.Cms.Abstractions.Mappings.Search;
using Mars.Cms.Abstractions.Services;
using Mars.Contracts.Common;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
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
public class PostTypeController : ControllerBase
{
    private readonly IPostTypeService _postTypeService;

    public PostTypeController(IPostTypeService postTypeService)
    {
        _postTypeService = postTypeService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostTypeDetailResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _postTypeService.GetDetail(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet("edit/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<PostTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        return _postTypeService.GetEditModel(id, cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PostTypeSummaryResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<ActionResult<PostTypeSummaryResponse>> Create([FromBody] CreatePostTypeRequest request, CancellationToken cancellationToken)
    {
        var created = await _postTypeService.Create(request.ToQuery(), cancellationToken);
        return Created("{id}", created.ToResponse());
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostTypeSummaryResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<PostTypeSummaryResponse> Update([FromBody] UpdatePostTypeRequest request, CancellationToken cancellationToken)
    {
        return (await _postTypeService.Update(request.ToQuery(), cancellationToken)).ToSummaryResponse();
    }

    [HttpGet("list/offset")]
    [AllowAnonymous]
    public async Task<ListDataResult<PostTypeListItemResponse>> List([FromQuery] ListPostTypeQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postTypeService.List(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("list/page")]
    [AllowAnonymous]
    public async Task<PagingResult<PostTypeListItemResponse>> ListTable([FromQuery] TablePostTypeQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postTypeService.ListTable(request.ToQuery(), cancellationToken)).ToResponse();
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
        return _postTypeService.Delete(id, cancellationToken);
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
        return _postTypeService.DeleteMany(new DeleteManyPostTypeQuery { Ids = ids }, cancellationToken);
    }

    [HttpGet("MetaFieldsTypeEnums")]
    public Dictionary<int, string> MetaFieldsTypeEnums()
    {

        var enums = Enum.GetValues<MetaFieldType>();
        //var dict = enums
        //       .Cast<EMetaFieldType>()
        //       .ToDictionary(t => (int)t, t => t.ToString()); вылетает сервер

        Dictionary<int, string> dict = [];

        foreach (var e in enums)
        {
            dict.Add((int)e, e.ToString());
        }

        return dict;
    }

    [HttpGet("AllMetaRelationsStructure")]
    public async Task<IReadOnlyCollection<MetaRelationModelResponse>> AllMetaRelationsStructure()
        => (await _postTypeService.AllMetaRelationsStructure()).ToResponse();

    [HttpGet("ListMetaValueRelationModels")]
    public async Task<ListDataResult<MetaValueRelationModelSummaryResponse>> ListMetaValueRelationModels([FromQuery] MetaValueRelationModelsListQueryRequest request, CancellationToken cancellationToken)
        => (await _postTypeService.ListMetaValueRelationModels(request.ToQuery(), cancellationToken)).ToResponse();

    [HttpGet("GetMetaValueRelationModels/{modelName}")]
    public async Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummaryResponse>> GetMetaValueRelationModels(string modelName, [FromQuery][MaxLength(100)] Guid[] ids, CancellationToken cancellationToken)
        => (await _postTypeService.GetMetaValueRelationModels(modelName, ids, cancellationToken)).ToDictionary(s => s.Key, s => s.Value.ToResponse());

    [HttpGet("presentation/edit/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public PostTypePresentationEditViewModel GetPresentationEditModel(Guid id, CancellationToken cancellationToken)
    {
        return _postTypeService.GetPresentationEditModel(id, cancellationToken) ?? throw new NotFoundException();
    }

    [HttpPut("presentation/update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task UpdatePresentation([FromBody] UpdatePostTypePresentationRequest request, CancellationToken cancellationToken)
    {
        await _postTypeService.UpdatePresentation(request.ToQuery(), cancellationToken);
    }
}
