using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class PostRepository : IPostRepository
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
                                            .ThenInclude(s => s.MetaField)
                                        .Include(s => s.Categories);

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
        _marsDbContext.Entry(entity).State = EntityState.Detached;

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
                                                .Include(s => s.PostPostCategories)
                                                .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
                                                ?? throw new NotFoundException();

        entity.UpdateEntity(query);

        if (query.MetaValues is not null)
        {
            MetaValuesTools.ModifyMetaValues(_marsDbContext, entity.MetaValues!, query.MetaValues, entity.ModifiedAt!.Value);
        }

        if (entity.PostType.TypeName != query.Type)
        {
            var newPostType = await _marsDbContext.PostTypes.FirstAsync(s => s.TypeName == query.Type);
            entity.PostTypeId = newPostType.Id;
        }
        await _marsDbContext.SaveChangesAsync(cancellationToken);
        _marsDbContext.Entry(entity).State = EntityState.Detached;
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

    public Task<int> DeleteMany(DeleteManyPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return _marsDbContext.Posts.Where(s => query.Ids.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
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
                                            .ThenInclude(s => s.MetaField)
                                        .Include(s => s.Categories);

        return (await list.ToListAsync(cancellationToken)).ToDetailList();
    }

    IQueryable<PostEntity> ListFilterQuery(ListPostQuery query)
    {
        var q = _listAllQuery.AsNoTracking()
                            .Include(s => s.PostType)
                            .Include(s => s.User)
                            .Where(s => query.Type == null || s.PostType.TypeName == query.Type);

        if (query.CategoryId is not null)
        {
            q = q.Include(s => s.Categories);
            if (query.FilterIncludeDescendantsCategories)
                q = q.Where(s => s.Categories!.Any(x => x.Path.Contains(query.CategoryId.ToString()!)));
            else
                q = q.Where(s => s.Categories!.Any(x => x.Id == query.CategoryId));
        }
        else if (query.IncludeCategory) q = q.Include(s => s.Categories);

        return q.Where(s => query.Search == null
                        || (EF.Functions.ILike(s.Id.ToString(), query.Search)
                            || EF.Functions.ILike(s.Slug, $"%{query.Search}%")
                            || EF.Functions.ILike(s.Title, $"%{query.Search}%")));
    }

    ListPostQuery RewriteSorting(ListPostQuery query, ref IQueryable<PostEntity> queryable)
    {
        if (query.Sort.IsNullOrEmpty()) return query;

        var sortColumnName = query.Sort.TrimStart('-');
        var desc = query.Sort.StartsWith('-');
        if (sortColumnName.Equals(nameof(PostListItemResponse.Categories), StringComparison.OrdinalIgnoreCase))
        {
            query = query with { Sort = null };
            queryable = desc
                        ? queryable.OrderByDescending(s => s.Categories!.FirstOrDefault().Slug)
                        : queryable.OrderBy(s => s.Categories!.FirstOrDefault().Slug);
        }
        else if (sortColumnName.Equals(nameof(PostListItemResponse.Author), StringComparison.OrdinalIgnoreCase))
        {
            query = query with { Sort = null };
            queryable = desc
                        ? queryable.OrderByDescending(s => s.User.UserName)
                        : queryable.OrderBy(s => s.User.UserName);
        }
        return query;
    }

    public async Task<ListDataResult<PostSummary>> List(ListPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);
        query = RewriteSorting(query, ref queryable);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(PostMapping.ToSummaryList);
    }

    public async Task<PagingResult<PostSummary>> ListTable(ListPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);
        query = RewriteSorting(query, ref queryable);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(PostMapping.ToSummaryList);
    }

    public async Task<ListDataResult<PostDetail>> ListDetail(ListPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        IQueryable<PostEntity> queryable = ListFilterQuery(query).Include(s => s.MetaValues!).ThenInclude(s => s.MetaField)
                                                                .Include(s=>s.Categories);
        query = RewriteSorting(query, ref queryable);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(PostMapping.ToDetailList);
    }

    public async Task<PagingResult<PostDetail>> ListTableDetail(ListPostQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        IQueryable<PostEntity> queryable = ListFilterQuery(query).Include(s => s.MetaValues!).ThenInclude(s => s.MetaField)
                                                                .Include(s => s.Categories);
        query = RewriteSorting(query, ref queryable);

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

    public Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken)
                        => _marsDbContext.Posts.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistAsync(string typeName, string slug, CancellationToken cancellationToken)
                        => _marsDbContext.Posts.AsNoTracking().Include(s => s.PostType).AnyAsync(s => s.PostType.TypeName == typeName && s.Slug == slug, cancellationToken);

}
