using System.ComponentModel;
using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Mappings.Files;
using Mars.Host.Shared.Mappings.Posts;
using Mars.Host.Shared.Mappings.PostTypes;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Files;
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
public class PostController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IFileService _fileService;
    private readonly IRequestContext _requestContext;
    private readonly IValidatorFabric _validatorFabric;

    public PostController(IPostService postService,
                            IFileService fileService,
                            IRequestContext requestContext,
                            IValidatorFabric validatorFabric)
    {
        _postService = postService;
        _fileService = fileService;
        _requestContext = requestContext;
        _validatorFabric = validatorFabric;
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

    [HttpGet("p/{type}/{slug}")]
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

    [HttpGet]
    [AllowAnonymous]
    public async Task<ListDataResult<PostListItemResponse>> List([FromQuery] ListPostQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postService.List(request.ToQuery(null), cancellationToken)).ToResponse();
    }

    [HttpGet("ListTable")]
    [AllowAnonymous]
    public async Task<PagingResult<PostListItemResponse>> ListTable([FromQuery] TablePostQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _postService.ListTable(request.ToQuery(null), cancellationToken)).ToResponse();
    }

    [HttpGet("{type}")]
    [AllowAnonymous]
    public async Task<ListDataResult<PostListItemResponse>> List([FromQuery] ListPostQueryRequest request,
                                                                    [DefaultValue("post")] string type,
                                                                    CancellationToken cancellationToken)
    {
        return (await _postService.List(request.ToQuery(type), cancellationToken)).ToResponse();
    }

    [HttpPost("ListTable/{type}")]
    [AllowAnonymous]
    public async Task<PagingResult<PostListItemResponse>> ListTable([FromQuery] TablePostQueryRequest request,
                                                                    [DefaultValue("post")] string type,
                                                                    CancellationToken cancellationToken)
    {
        return (await _postService.ListTable(request.ToQuery(type), cancellationToken)).ToResponse();
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

    //------------ Extra
    //------------ Extra
    //------------ Extra
    //------------ Extra
    //--
    //--
    //--
    //--
    //--
    //--
    //--

    //[HttpGet(nameof(GetBlank) + "/{postTypeName}")]
    //public async Task<Post> GetBlank(string postTypeName)
    //{
    //    throw new NotImplementedException();
    //    //return await modelService.GetBlank(postTypeName);
    //}

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
        await _validatorFabric.ValidateAndThrowAsync<UploadMediaFileValidator, IFormFile>(file, cancellationToken);

        //FileEntity fileEntity = _fileService.WriteUpload(file, EFileType.PostAttachment, file_group);
        Guid userId = Guid.Empty;
        var createdFileId = await _fileService.WriteUpload(file, "Posts", userId, cancellationToken);
        var fileDetail = await _fileService.GetDetail(createdFileId, cancellationToken) ?? throw new InvalidOperationException("file not written");
        var action = UserActionResult<FileDetailResponse>.Success(fileDetail.ToResponse(), "ok");

        //return new ResponseUploadFile(fileEntity);

        return action;
    }

    /*
    ///////////////////////////////////////
    //Comments
    [HttpGet(nameof(Comments) + "/{id:guid}")]
    public async Task<IEnumerable<CommentDto>> Comments(Guid id)
    {
        CheckPostTypeFeature(nameof(AppShared.Models.Post.Comments));
        var a = await modelService.Comments(id);
        return a;
    }

    [Authorize]
    [HttpPost(nameof(Comments) + "/{id:guid}")]
    public async Task<UserActionResult<Comment>> AddComment(Guid id, CommentAddDto dto)
    {
        CheckPostTypeFeature(nameof(AppShared.Models.Post.Comments));
        return await modelService.AddComment(dto);
    }

    [Authorize]
    [HttpDelete(nameof(Comments) + "/{id:guid}/{commentId:guid}")]
    public async Task<UserActionResult> RemoveComment(Guid id, Guid commentId)
    {
        CheckPostTypeFeature(nameof(AppShared.Models.Post.Comments));
        return await modelService.RemoveComment(id, commentId);
    }

    ///////////////////////////////////////
    //Likes
    [Authorize]
    [HttpGet(nameof(LikedUsers) + "/{id:guid}")]
    public async Task<IEnumerable<UserDto>> LikedUsers(Guid id)
    {
        CheckPostTypeFeature(nameof(AppShared.Models.Post.Likes));
        return await modelService.LikedUsers(id);
    }

    [Authorize]
    [HttpPut(nameof(LikePost) + "/{id:guid}")]
    public async Task<UserLikeResult> LikePost(Guid id)
    {
        CheckPostTypeFeature(nameof(AppShared.Models.Post.Likes));
        return await modelService.LikePost(id);
    }

    [Authorize]
    [HttpPut(nameof(UnlikePost) + "/{id:guid}")]
    public async Task<UserLikeResult> UnlikePost(Guid id)
    {
        CheckPostTypeFeature(nameof(AppShared.Models.Post.Likes));
        return await modelService.UnlikePost(id);
    }

    //
    [Authorize(Roles = "Admin")]
    [HttpPost(nameof(ImportData) + "/json/{postType}")]
    public async Task<UserActionResult> ImportData(string postType, [FromBody] JArray json)
    {
        return modelService.ImportData(postType, json);
    }
    */

}
