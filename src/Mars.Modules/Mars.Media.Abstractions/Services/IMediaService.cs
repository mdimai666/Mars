using Mars.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace Mars.Media.Abstractions.Services;

public interface IMediaService : IFileService
{
    Task<UserActionResult> ScanFilesAndSaveInDB(Guid userId, CancellationToken cancellationToken);
    Task<UserActionResult> GenerateThumbnails(bool onlyWithEmptyMeta, CancellationToken cancellationToken);
    Task<Guid> WriteUploadToMedia(IFormFile formFile, Guid userId, CancellationToken cancellationToken, Guid? folderId = null, string? folderPath = null);
}
