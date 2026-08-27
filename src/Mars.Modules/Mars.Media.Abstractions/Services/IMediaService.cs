using Mars.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace Mars.Media.Abstractions.Services;

public interface IMediaService : IFileService
{
    Task<UserActionResult> ExecuteAction(ExecuteActionRequest action, Guid userId, CancellationToken cancellationToken);
    Task<Guid> WriteUploadToMedia(IFormFile formFile, Guid userId, CancellationToken cancellationToken, Guid? folderId = null, string? folderPath = null);
}
