using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class PostRepository : IPostRepository, IDisposable
{
    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    IQueryable<PostEntity> _listAllQuery => _marsDbContext.Posts.OrderByDescending(s => s.CreatedAt);

    public PostRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<PostSummary?> Get(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.Posts.AsNoTracking()
                                        .Include(s => s.PostType)
                                        .Include(s => s.User)
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                        ?.ToSummary();

    IQueryable<PostEntity> InternalDetail => _marsDbContext.Posts.AsNoTracking()
                                        .Include(s => s.PostType)
                                        .Include(s => s.User)
                                        .Include(s => s.MetaValues!)
                                            .ThenInclude(s => s.MetaField);

    public async Task<PostDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
                                => (await InternalDetail
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                        ?.ToDetail();

    public async Task<PostDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken)
                                => (await InternalDetail
                                        .FirstOrDefaultAsync(s => s.PostType.TypeName == type
                                                            && EF.Functions.ILike(s.Slug, slug), cancellationToken))
                                        ?.ToDetail();

    public async Task<PostEditDetail?> GetPostEditDetail(Guid id, CancellationToken cancellationToken)
                                => (await InternalDetail
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                        ?.ToEditDetail();

    public async Task<Guid> Create(CreatePostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var postType = await _marsDbContext.PostTypes.FirstAsync(s => s.TypeName == query.Type);

        var entity = query.ToEntity(postType.Id);

        await _marsDbContext.Posts.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task Update(UpdatePostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.Posts.Include(s => s.PostType)
                                                .Include(s => s.MetaValues!)
                                                    .ThenInclude(s => s.MetaField)
                                                .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
                                                ?? throw new NotFoundException();
        var featureActive = (string featureName) => entity.PostType.EnabledFeatures.Contains(featureName);

        entity.Title = query.Title;
        entity.Slug = query.Slug;
        //if(featureActive(PostTypeConstants.Features.Tags))
        entity.Tags = query.Tags.ToList();
        entity.Content = query.Content;
        entity.Excerpt = query.Excerpt;
        entity.Status = query.Status ?? "";
        entity.LangCode = query.LangCode;

        entity.ModifiedAt = DateTimeOffset.Now;
        MetaValuesTools.ModifyMetaValues(_marsDbContext, entity.MetaValues!, query.MetaValues, entity.ModifiedAt.Value);

        if (entity.PostType.TypeName != query.Type)
        {
            var newPostType = await _marsDbContext.PostTypes.FirstAsync(s => s.TypeName == query.Type);
            entity.PostTypeId = newPostType.Id;
        }

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.Posts.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
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

    private IQueryable<PostEntity> ListAllInternal(ListAllPostQuery query)
    {
        return _listAllQuery.AsNoTracking()
                            .Include(s => s.PostType)
                            .Include(s => s.User)
                            .Where(s => (query.Ids == null || query.Ids.Contains(s.Id))
                                        && (query.Type == null || s.PostType.TypeName == query.Type));
    }

    public async Task<IReadOnlyCollection<PostSummary>> ListAll(ListAllPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var list = ListAllInternal(query);

        return (await list.ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<IReadOnlyCollection<PostDetail>> ListAllDetail(ListAllPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var list = ListAllInternal(query).Include(s => s.MetaValues!)
                                            .ThenInclude(s => s.MetaField);

        return (await list.ToListAsync(cancellationToken)).ToDetailList();
    }

    //public async Task<IReadOnlyCollection<PostSummary>> ListAllActive(CancellationToken cancellationToken)
    //{
    //    cancellationToken.ThrowIfCancellationRequested();
    //    ThrowIfDisposed();

    //    return (await _listAllQuery.AsNoTracking()
    //                                .Include(s => s.PostType)
    //                                .Include(s => s.User)
    //                                .Where(s => s.DeletedAt != null)
    //                                .ToListAsync(cancellationToken)).ToSummaryList();
    //}

    IQueryable<PostEntity> ListFilterQuery(ListPostQuery query) => _listAllQuery
                                    .AsNoTracking()
                                    .Include(s => s.PostType)
                                    .Include(s => s.User)
                                    .Where(s => query.Type == null || s.PostType.TypeName == query.Type)
                                    .Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Slug, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Title, $"%{query.Search}%")));

    public async Task<ListDataResult<PostSummary>> List(ListPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(PostMapping.ToSummaryList);

    }

    public async Task<PagingResult<PostSummary>> ListTable(ListPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(PostMapping.ToSummaryList);

    }

    public async Task<ListDataResult<PostDetail>> ListDetail(ListPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query).Include(s => s.MetaValues!)
                                            .ThenInclude(s => s.MetaField);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(PostMapping.ToDetailList);

    }

    public async Task<PagingResult<PostDetail>> ListTableDetail(ListPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query).Include(s => s.MetaValues!)
                                            .ThenInclude(s => s.MetaField);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(PostMapping.ToDetailList);

    }

    //============================
    //Extra methods
    public async Task<PostDetailWithType?> PostDetailWithType(Guid id, CancellationToken cancellationToken)
                                    => (await InternalDetail
                                        .Include(s => s.PostType)
                                            .ThenInclude(s => s.MetaFields)
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                        ?.ToDetailWithType();
}
