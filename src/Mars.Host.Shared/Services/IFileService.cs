using Mars.Host.Shared.Dto.Files;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Shared.Services;

public interface IFileService
{
    Task<FileDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<ListDataResult<FileListItem>> List(ListFileQuery query, CancellationToken cancellationToken);
    Task<PagingResult<FileListItem>> ListTable(ListFileQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken);
    Task Update(UpdateFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task UpdateBulk(IReadOnlyCollection<UpdateFileQuery> query, FileHostingInfo hostingInfo, CancellationToken cancellationToken);

    Task<Guid> WriteUpload(string originalFileNameWithExt, string subpath, byte[] bytes, Guid userId, CancellationToken cancellationToken);

    Task<Guid> WriteUpload(IFormFile formFile, string subpath, Guid userId, CancellationToken cancellationToken);
    Task<Guid> WriteUpload(string originalFileNameWithExt, string subpath, Stream fileStream, Guid userId, CancellationToken cancellationToken);

}
