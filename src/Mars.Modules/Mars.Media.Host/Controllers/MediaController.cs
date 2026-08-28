using System.Net.Mime;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Services;
using Mars.Server.Abstractions.Validators;
using Mars.Media.Abstractions.Mappings.Files;
using Mars.Contracts.Common;
using Mars.Media.Contracts.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Media.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly IMediaFolderService _folderService;
    private readonly IRequestContext _requestContext;
    private readonly IValidatorFactory _validatorFactory;

    public MediaController(
        IMediaService mediaService,
        IMediaFolderService folderService,
        IRequestContext requestContext,
        IValidatorFactory validatorFactory)
    {
        _mediaService = mediaService;
        _folderService = folderService;
        _requestContext = requestContext;
        _validatorFactory = validatorFactory;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<FileDetailResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _mediaService.GetDetail(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet("list/offset")]
    public async Task<ListDataResult<FileListItemResponse>> List([FromQuery] ListFileQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _mediaService.List(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("list/page")]
    public async Task<PagingResult<FileListItemResponse>> ListTable([FromQuery] TableFileQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _mediaService.ListTable(request.ToQuery(), cancellationToken)).ToResponse();
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
        return _mediaService.Delete(id, cancellationToken);
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
        return _mediaService.DeleteMany(new DeleteManyFileQuery { Ids = ids }, cancellationToken);
    }

    //public async override Task<ActionResult<TotalResponse<FileEntity>>> ListTable([NotNull] QueryFilter filter)
    //{
    //    var (user, isAdmin) = await modelService.GetCurrentUserIsAdmin();
    //    if (isAdmin)
    //    {
    //        return await modelService.ListTable(filter, null, s => s.User);
    //    }
    //    else if (user is not null)
    //    {
    //        return await modelService.ListTable(filter, s => s.UserId == user.Id, s => s.User);
    //    }
    //    else
    //    {
    //        return new TotalResponse<FileEntity>
    //        {
    //            Records = Array.Empty<FileEntity>(),
    //            Result = ETotalResponeResult.OK,
    //            TotalCount = 0,
    //        };
    //    }
    //}

    [HttpPost("Upload")]
    [RequestSizeLimit(2_147_483_648)]//2GB
    public async Task<ActionResult<FileDetailResponse>> Upload(
            IFormFile file,
            //[FromQuery] string file_group = "Files",
            [FromQuery] Guid? folderId = null,
            [FromQuery] string? folderPath = null,
            CancellationToken cancellationToken = default)
    {
        await _validatorFactory.ValidateAndThrowAsync<IFormFile, UploadMediaFileValidator>(file, cancellationToken);

        var fileId = await _mediaService.WriteUploadToMedia(file, _requestContext.User.Id, cancellationToken, folderId, folderPath);
        return (await _mediaService.GetDetail(fileId, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet("folders")]
    public async Task<List<FolderResponse>> ListFolders([FromQuery] Guid? parentId, CancellationToken cancellationToken)
    {
        return (await _folderService.ListFolders(parentId, cancellationToken)).ToResponseList();
    }

    [HttpGet("folders/{id:guid}/breadcrumbs")]
    public async Task<List<FolderResponse>> FolderBreadcrumbs(Guid id, CancellationToken cancellationToken)
    {
        return (await _folderService.GetBreadcrumbs(id, cancellationToken)).ToResponseList();
    }

    [HttpPost("folders")]
    public async Task<FolderResponse> CreateFolder([FromBody] CreateFolderRequest request, CancellationToken cancellationToken)
    {
        var query = new CreateFolderQuery
        {
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
            UserId = _requestContext.User.Id,
        };
        return (await _folderService.Create(query, cancellationToken)).ToResponse();
    }

    [HttpPut("folders/{id:guid}/rename")]
    public async Task<FolderResponse> RenameFolder(Guid id, [FromBody] RenameFolderRequest request, CancellationToken cancellationToken)
    {
        return (await _folderService.Rename(id, request.NewName.Trim(), cancellationToken)).ToResponse();
    }

    [HttpDelete("folders/{id:guid}")]
    public Task DeleteFolder(Guid id, CancellationToken cancellationToken)
    {
        return _folderService.Delete(id, cancellationToken);
    }

    [HttpPost("move-files")]
    public Task<UserActionResult> MoveFiles([FromBody] MoveFilesRequest request, CancellationToken cancellationToken)
    {
        return _folderService.MoveFiles(new MoveFilesQuery { Ids = request.Ids, FolderId = request.FolderId }, cancellationToken);
    }

    [HttpPost(nameof(Upload2))]
    [RequestSizeLimit(150_000_000)]
    public Task<ActionResult<UserActionResult<List<FileDetailResponse>>>> Upload2(IFormFileCollection files, [FromQuery] string file_group = "Files")
    {

        //try
        //{
        //    //var files = HttpContext.Request.Form.Files;

        //    List<FileEntity> added = new();

        //    foreach (var _file in files)
        //    {
        //        var file = _file;
        //        FileEntity f = modelService.WriteUpload(file, EFileType.Media, file_group);
        //        added.Add(f);
        //    }

        //    return new UserActionResult<List<FileEntity>>
        //    {
        //        Ok = true,
        //        Message = "Успешно добавлено",
        //        Data = added
        //    };
        //}
        //catch (Exception ex)
        //{
        //    return new UserActionResult<List<FileEntity>>
        //    {
        //        Message = ex.Message
        //    };
        //}
        throw new NotImplementedException();
    }

    //[Authorize]
    //[HttpDelete(nameof(DeleteFileEntity) + "/{id:guid}")]
    //public async Task<ActionResult<UserActionResult>> DeleteFileEntity(Guid id)
    //{
    //    return await modelService.Delete(id);
    //}

    [HttpPost("ExecuteAction")]
    public async Task<UserActionResult> ExecuteAction(ExecuteActionRequest action, CancellationToken cancellationToken)
    {
        return await _mediaService.ExecuteAction(action, _requestContext.User.Id, cancellationToken);
    }
}
