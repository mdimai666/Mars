using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Mappings.Files;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

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
    private readonly IRequestContext _requestContext;
    private readonly IValidatorFabric _validatorFabric;

    public MediaController(IMediaService mediaService, IRequestContext requestContext, IValidatorFabric validatorFabric)
    {
        _mediaService = mediaService;
        _requestContext = requestContext;
        _validatorFabric = validatorFabric;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<FileDetailResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _mediaService.GetDetail(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpGet]
    public async Task<ListDataResult<FileListItemResponse>> List([FromQuery] ListFileQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _mediaService.List(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("ListTable")]
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
            CancellationToken cancellationToken = default)
    {
        await _validatorFabric.ValidateAndThrowAsync<IFormFile, UploadMediaFileValidator>(file, cancellationToken);

        var fileId = await _mediaService.WriteUploadToMedia(file, _requestContext.User.Id, cancellationToken);
        return (await _mediaService.GetDetail(fileId, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
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
