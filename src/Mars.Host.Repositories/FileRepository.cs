using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class FileRepository : IFileRepository
{
    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    IQueryable<FileEntity> _listAllQuery => _marsDbContext.Files.OrderByDescending(s => s.CreatedAt);


    public FileRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<FileSummary?> Get(Guid id, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        var resolver = new ImagePreviewResolver(new(), hostingInfo);
        return (await _marsDbContext.Files.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToSummary(resolver);
    }

    public async Task<FileDetail?> GetDetail(Guid id, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        var resolver = new ImagePreviewResolver(new(), hostingInfo);
        return (await _marsDbContext.Files.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToDetail(resolver);
    }

    public async Task<Guid> Create(CreateFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = query.ToEntity(hostingInfo);

        await _marsDbContext.Files.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task CreateMany(IReadOnlyCollection<CreateFileQuery> queries, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(queries, nameof(queries));

        var entities = queries.Select(q => q.ToEntity(hostingInfo));

        await _marsDbContext.Files.AddRangeAsync(entities, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Update(UpdateFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.Files.FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Entry(entity).CurrentValues.SetValues(new
        {
            FileName = query.Name,
            Meta = query.Meta?.ToEntity(hostingInfo),
        });
        entity.ModifiedAt = DateTimeOffset.Now;

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBulk(IReadOnlyCollection<UpdateFileQuery> query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var ids = query.Select(s => s.Id).ToList();
        var entities = await _marsDbContext.Files.Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);

        if (entities.Count != ids.Count) throw new NotFoundException("some elements not found");

        var modifiedDate = DateTimeOffset.Now;
        var queryDict = query.ToDictionary(s => s.Id);

        //_marsDbContext.Entry(entity).CurrentValues.SetValues(new
        //{
        //    FileName = query.Name,
        //    Meta = query.Meta?.ToEntity(hostingInfo),
        //});

        entities.ForEach(e =>
        {
            e.FileName = queryDict[e.Id].Name;
            if (queryDict[e.Id].Meta != null)
            {
                e.Meta = queryDict[e.Id].Meta!.ToEntity(hostingInfo);
            }
            e.ModifiedAt = DateTimeOffset.Now;
        });

        //_marsDbContext.Files.ExecuteUpdate(s => s
        //                .SetProperty(p => p.ModifiedAt, modifiedDate)
        //                .SetProperty(p => p.FileName, p => queryDict[p.Id].Name)
        //                .SetProperty(p => p.Meta, p => queryDict[p.Id].Meta!.ToEntity(hostingInfo))
        //);

        //entity.ModifiedAt = DateTimeOffset.Now;

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.Files.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteBulk(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var deletedCount = await _marsDbContext.Files.Where(s => ids.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
        return deletedCount;
    }

    /// <summary>
    /// Throws if this class has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedThrowHelper.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Dispose the store
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }

    private async Task<List<FileEntity>> ListAllInternal(ListAllFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var que = _listAllQuery.AsNoTracking();

        if (query.Ids is not null)
        {
            que = que.Where(s => query.Ids.Contains(s.Id));
        }

        var list = await que.ToListAsync(cancellationToken);

        if (query.IsImage is not null)//TODO: save ext in database and replace to queryable
        {
            list = list.Where(s => hostingInfo.ExtIsImage(s.FileExt)).ToList();
        }

        return list;
    }

    public async Task<IReadOnlyCollection<FileListItem>> ListAll(ListAllFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        var resolver = new ImagePreviewResolver(new(), hostingInfo);
        return (await ListAllInternal(query, hostingInfo, cancellationToken)).ToListItemList(resolver);
    }

    public async Task<IReadOnlyCollection<FileDetail>> ListAllDetail(ListAllFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        var resolver = new ImagePreviewResolver(new(), hostingInfo);
        return (await ListAllInternal(query, hostingInfo, cancellationToken)).ToDetailList(resolver);
    }

    public async Task<IReadOnlyCollection<string>> ListAllAbsolutePaths(FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var list = await _listAllQuery.AsNoTracking().ToListAsync(cancellationToken);

        return list.Select(file => hostingInfo.FileAbsolutePath(file.FilePhysicalPath)).ToList();
    }

    public async Task<ListDataResult<FileListItem>> List(ListFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.FileName, $"%{query.Search}%")));

        var list = await queryable.ToListDataResult(query, cancellationToken);

        var resolver = new ImagePreviewResolver(new(), hostingInfo);
        return list.ToListItemList(resolver);

    }

    public async Task<PagingResult<FileListItem>> ListTable(ListFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.FileName, $"%{query.Search}%")));

        var list = await queryable.ToPagingResult(query, cancellationToken);

        var resolver = new ImagePreviewResolver(new(), hostingInfo);
        return list.ToListItemList(resolver);

    }
}
