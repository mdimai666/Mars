using Mars.Contracts.Common;
using Mars.Contracts.Dto.Files;
using Mars.Media.Abstractions.Dto.Files;

namespace Mars.Media.Abstractions.Repositories;

public interface IFileRepository : IDisposable
{
    Task<FileSummary?> Get(Guid id, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task<FileDetail?> GetDetail(Guid id, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task<FileDetail?> GetFileByPathDetail(string filePath, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> FileExistByPath(string filePath, CancellationToken cancellationToken);
    Task<Guid> Create(CreateFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task CreateMany(IReadOnlyCollection<CreateFileQuery> queries, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task Update(UpdateFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task UpdateBulk(IReadOnlyCollection<UpdateFileQuery> query, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task UpdateAfterMove(IReadOnlyCollection<FileMoveUpdate> updates, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task<int> CountByFolder(Guid folderId, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
    Task<int> DeleteMany(DeleteManyFileQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FileListItem>> ListAll(ListAllFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FileDetail>> ListAllDetail(ListAllFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> ListAllAbsolutePaths(FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task<ListDataResult<FileListItem>> List(ListFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
    Task<PagingResult<FileListItem>> ListTable(ListFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken);
}
