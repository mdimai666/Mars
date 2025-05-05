using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Shared.Services;

public interface IMediaService : IFileService
{
    Task<UserActionResult> ExecuteAction(ExecuteActionRequest action, Guid userId, CancellationToken cancellationToken);
    Task<Guid> WriteUploadToMedia(IFormFile formFile, Guid userId, CancellationToken cancellationToken);
}
