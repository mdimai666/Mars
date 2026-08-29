using System.ComponentModel;
using System.Net.Mime;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Mappings.Posts;
using Mars.Cms.Abstractions.Mappings.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.Posts;
using Mars.Contracts.Common;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Mappings.Files;
using Mars.Media.Abstractions.Services;
using Mars.Media.Contracts.Files;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Server.Abstractions.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Cms.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IFileService _fileService;
    private readonly IValidatorFactory _validatorFactory;
    private readonly IPostMetaColumnsService _postMetaColumnsService;

    public PostController(IPostService postService,
                            IFileService fileService,
                            IValidatorFactory validatorFactory,
                            IPostMetaColumnsService postMetaColumnsService)
    {
        _postService = postService;
        _fileService = fileService;
        _validatorFactory = validatorFactory;
        _postMetaColumnsService = postMetaColumnsService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostDetailResponse> Get(Guid id, bool renderContent = true, CancellationToken cancellationToken = default)
    {
        return (await _postService.GetDetail(id, renderContent: renderContent, cancellationToken))?.ToResponse()
                ?? throw new NotFoundException();
    }

    [HttpGet("by-type/{type}/item/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PostDetailResponse> GetBySlug(string slug,
                                                    [DefaultValue("post")] string type,
                                                    bool renderContent = true,
                                                    CancellationToken cancellationToken = default)
    {
        return (await _postService.GetDetailBySlug(slug, type, renderContent: renderContent, cancellationToken))?.ToResponse()
                ?? throw new NotFoundException();
    }

    [HttpGet("edit/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<PostEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        return _postService.GetEditModel(id, cancellationToken);
    }

    [HttpGet("edit/blank/{type}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<PostEditViewModel> GetEditModelBlank([DefaultValue("post")] string type, CancellationToken cancellationToken)
    {
        return _postService.GetEditModelBlank(type, cancellationToken);
    }

    [HttpGet("single/{type}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<PostDetailResponse> GetOrCreateSingle(string type, CancellationToken cancellationToken)
    {
        return (await _postService.GetOrCreateSingleAsync(type, cancellationToken)).ToResponse();
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
    public async Task<ActionResult<PostDetailResponse>> Create([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
    {
        var query = _postService.EnrichQuery(request);
        var created = await _postService.Create(query, cancellationToken);
        return Created("{id}", created.ToResponse());
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
        var query = _postService.EnrichQuery(request);
        return (await _postService.Update(query, cancellationToken)).ToResponse();
    }

    [HttpGet("list/offset")]
    [AllowAnonymous]
    public async Task<ListDataResult<PostListItemResponse>> List([FromQuery] ListPostQueryRequest request, CancellationToken cancellationToken)
    {
        var result = (await _postService.List(request.ToQuery(null), cancellationToken)).ToResponse();
        return await EnrichMetaColumnsAsync(result, null, request.MetaFields, cancellationToken);
    }

    [HttpGet("list/page")]
    [AllowAnonymous]
    public async Task<PagingResult<PostListItemResponse>> ListTable([FromQuery] TablePostQueryRequest request, CancellationToken cancellationToken)
    {
        var result = (await _postService.ListTable(request.ToQuery(null), cancellationToken)).ToResponse();
        return await EnrichMetaColumnsAsync(result, null, request.MetaFields, cancellationToken);
    }

    [HttpGet("by-type/{type}/list/offset")]
    [AllowAnonymous]
    public async Task<ListDataResult<PostListItemResponse>> List([FromQuery] ListPostQueryRequest request,
                                                                    [DefaultValue("post")] string type,
                                                                    CancellationToken cancellationToken)
    {
        var result = (await _postService.List(request.ToQuery(type), cancellationToken)).ToResponse();
        return await EnrichMetaColumnsAsync(result, type, request.MetaFields, cancellationToken);
    }

    [HttpGet("by-type/{type}/list/page")]
    [AllowAnonymous]
    public async Task<PagingResult<PostListItemResponse>> ListTable([FromQuery] TablePostQueryRequest request,
                                                                    [DefaultValue("post")] string type,
                                                                    CancellationToken cancellationToken)
    {
        var result = (await _postService.ListTable(request.ToQuery(type), cancellationToken)).ToResponse();
        return await EnrichMetaColumnsAsync(result, type, request.MetaFields, cancellationToken);
    }

    /// <summary>Прикладывает к элементам списка отображаемые значения запрошенных мета-полей (колонки грида)</summary>
    async Task<ListDataResult<PostListItemResponse>> EnrichMetaColumnsAsync(ListDataResult<PostListItemResponse> result,
                                                                             string? typeName,
                                                                             string[]? metaFields,
                                                                             CancellationToken cancellationToken)
    {
        if (metaFields is not { Length: > 0 } || result.Items.Count == 0) return result;

        var values = await LoadMetaColumnsAsync(result.Items, typeName, metaFields, cancellationToken);
        var items = result.Items
                          .Select(item => item with { MetaColumns = values.GetValueOrDefault(item.Id) })
                          .ToList();

        return new ListDataResult<PostListItemResponse>(items, result.HasMoreData, result.TotalCount);
    }

    /// <summary>Прикладывает к элементам списка отображаемые значения запрошенных мета-полей (колонки грида)</summary>
    async Task<PagingResult<PostListItemResponse>> EnrichMetaColumnsAsync(PagingResult<PostListItemResponse> result,
                                                                          string? typeName,
                                                                          string[]? metaFields,
                                                                          CancellationToken cancellationToken)
    {
        if (metaFields is not { Length: > 0 } || result.Items.Count == 0) return result;

        var values = await LoadMetaColumnsAsync(result.Items, typeName, metaFields, cancellationToken);
        var items = result.Items
                          .Select(item => item with { MetaColumns = values.GetValueOrDefault(item.Id) })
                          .ToList();

        return new PagingResult<PostListItemResponse>(items, result.Page, result.PageSize, result.HasMoreData, result.TotalCount);
    }

    /// <summary>Батч-загрузка значений: тип из маршрута либо группировка по типам самих элементов</summary>
    async Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string?>>> LoadMetaColumnsAsync(
        IEnumerable<PostListItemResponse> items,
        string? typeName,
        string[] metaFields,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<Guid, IReadOnlyDictionary<string, string?>>();
        foreach (var group in items.GroupBy(s => typeName ?? s.Type))
        {
            var part = await _postMetaColumnsService.GetDisplayValuesAsync(
                group.Key, metaFields, group.Select(s => s.Id).ToList(), cancellationToken);
            foreach (var pair in part)
            {
                values[pair.Key] = pair.Value;
            }
        }

        return values;
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
        return _postService.Delete(id, cancellationToken);
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
        return _postService.DeleteMany(new DeleteManyPostQuery { Ids = ids }, cancellationToken);
    }

    [Authorize]
    [HttpPost(nameof(Upload))]
    [RequestSizeLimit(2_147_483_648)]//2GB
    public async Task<UserActionResult<FileDetailResponse>> Upload(
                IFormFile file,
                [FromForm] Guid id,
                //[FromQuery] string file_group = "Files",
                CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID is empty");
        await _validatorFactory.ValidateAndThrowAsync<IFormFile, UploadMediaFileValidator>(file, cancellationToken);

        //FileEntity fileEntity = _fileService.WriteUpload(file, EFileType.PostAttachment, file_group);
        Guid userId = Guid.Empty;
        var createdFileId = await _fileService.WriteUpload(file, "Posts", userId, cancellationToken);
        var fileDetail = await _fileService.GetDetail(createdFileId, cancellationToken) ?? throw new InvalidOperationException("file not written");
        var action = UserActionResult<FileDetailResponse>.Success(fileDetail.ToResponse(), "ok");

        //return new ResponseUploadFile(fileEntity);

        return action;
    }

}
